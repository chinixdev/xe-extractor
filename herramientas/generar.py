# -*- coding: utf-8 -*-
"""Genera, desde Z:\\casos\\<caso>\\{analysis_input,trace,metadata}.json:
   - Z:\\casos\\<caso>\\resumen.md  (pequeno: sps, tablas, columnas usadas / no usadas)
   - visor\\datos.js               (datos de todos los casos para visor\\index.html)
Uso:  python herramientas\\generar.py [carpeta_casos]     (por defecto Z:\\casos o %ERP_CASOS%)
"""
import json, os, re, sys, glob, collections

BASE = sys.argv[1] if len(sys.argv) > 1 else os.environ.get('ERP_CASOS') or (r'Z:\casos' if os.path.isdir(r'Z:\casos') else 'casos')
VISOR = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'visor')
NOISE_TABLES = {'inserted', 'deleted', 'into', 'from'}


def load(p):
    return json.load(open(p, encoding='utf-8'))


def split_top(s):
    out, cur, depth, q = [], '', 0, False
    for ch in s:
        if ch == "'":
            q = not q
        if not q:
            if ch == '(': depth += 1
            elif ch == ')': depth -= 1
            elif ch == ',' and depth == 0:
                out.append(cur.strip()); cur = ''; continue
        cur += ch
    if cur.strip(): out.append(cur.strip())
    return out


def parse_case(caso):
    d = os.path.join(BASE, caso)
    ana = load(os.path.join(d, 'analysis_input.json'))
    tr = load(os.path.join(d, 'trace.json'))
    meta = load(os.path.join(d, 'metadata.json')) if os.path.exists(os.path.join(d, 'metadata.json')) else {'Objects': []}
    mt = {o['Name'].lower(): o for o in meta['Objects'] if o['Type'] == 'USER_TABLE'}
    sess = {o['SessionId'] for o in ana['Operations']}

    flujo = []
    for o in ana['Operations']:
        if o.get('Via'): continue
        ts = o['Timestamp']
        base = [ts[11:19], ts[:10], o['SessionId']]
        sq = re.sub(r'\s+', ' ', o['Sql']).strip()
        if o['Operation'] == 'EXEC': flujo.append(base + [o['Objects'][0], sq[:230], o['Count'], ''])
        elif o['Operation'] != 'SELECT': flujo.append(base + ['(aplicacion)', sq[:230], o['Count'], o['Operation'] + ' ' + ', '.join(o['Objects'])])
    # procedures
    ejec, llam, calls, ex = collections.Counter(), collections.Counter(), set(), collections.defaultdict(list)
    for o in ana['Operations']:
        if o['Operation'] != 'EXEC': continue
        name = o['Objects'][0]
        via = o.get('Via')
        if via:
            llam[name] += o['Count']; calls.add((via, name))
        else:
            ejec[name] += o['Count']
            if len(ex[name]) < 2: ex[name].append(re.sub(r'\s+', ' ', o['Sql'])[:170])

    # escrituras desde trace (columnas completas)
    writes = collections.defaultdict(lambda: {'ops': set(), 'cols': collections.OrderedDict(), 'procs': set(), 'nocols': False})
    seen = set()
    for s in tr['Sessions']:
        if s['SessionId'] not in sess or s['Application'].startswith('Microsoft SQL Server Management Studio'): continue
        for e in s['Events']:
            if e['Event'] == 'sql_batch_completed': continue
            q = e['Sql'].strip()
            via = e.get('Via') or '(aplicacion)'
            key = (q, via, e['Time'][:19])
            if key in seen: continue
            seen.add(key)

            def reg(tab, op, cols):
                if tab.lower() in NOISE_TABLES or tab.startswith(('#', '@')) or len(tab) <= 2: return
                w = writes[tab.lower()]
                w.setdefault('name', mt[tab.lower()]['Name'] if tab.lower() in mt else tab)
                w['ops'].add(op); w['procs'].add(via)
                if cols is None: w['nocols'] = True
                else:
                    for c in cols: w['cols'].setdefault(c.lower(), c)

            m = re.match(r'(?is)insert\s+(?:into\s+)?\[?([\w\.]+)\]?\s*\((.*?)\)\s*(?:values|select)', q)
            if m:
                reg(m.group(1).split('.')[-1], 'INSERT', [re.sub(r'--[^\n]*|/\*.*?\*/', '', c).strip().strip('[]') for c in m.group(2).split(',')]); continue
            m = re.match(r'(?is)insert\s+(?:into\s+)?\[?([\w\.]+)\]?\s+(?:select|values|exec)', q)
            if m: reg(m.group(1).split('.')[-1], 'INSERT', None); continue
            m = re.match(r'(?is)update\s+\[?([\w\.]+)\]?\s+set\s+(.*?)(\bfrom\b.*|\bwhere\b.*|$)', q)
            if m:
                tab = m.group(1).split('.')[-1]
                if tab.lower() not in mt:  # alias: UPDATE a SET ... FROM tabla a
                    mm = re.search(r'(?i)\bfrom\s+(?:dbo\.)?\[?(\w+)\]?\s+(?:as\s+)?' + re.escape(tab) + r'\b', q) or re.search(r'(?i)\bjoin\s+(?:dbo\.)?\[?(\w+)\]?\s+(?:as\s+)?' + re.escape(tab) + r'\b', q)
                    if mm: tab = mm.group(1)
                cols = []
                for part in split_top(re.sub(r'--[^\n]*|/\*.*?\*/', '', m.group(2))):
                    mm = re.match(r'(?:\w+\.)?\[?(\w+)\]?\s*=', part)
                    if mm: cols.append(mm.group(1))
                reg(tab, 'UPDATE', cols); continue
            m = re.match(r'(?is)delete\s+(?:from\s+)?\[?([\w\.]+)\]?(?:\s+from\s+(?:dbo\.)?\[?(\w+)\]?)?', q)
            if m:
                tab = m.group(1).split('.')[-1]
                if tab.lower() not in mt and m.group(2): tab = m.group(2)
                reg(tab, 'DELETE', []); continue

    # lecturas (resumen de analysis_input)
    reads = {}
    for s in ana['Summary']:
        if s['ObjectType'] == 'TABLE' and s['ObjectName'].lower() not in writes and s['ObjectName'].lower() not in NOISE_TABLES:
            reads[s['ObjectName']] = s['Executions']

    # aristas procedure -> tabla
    edges = []
    for t, w in writes.items():
        for p in sorted(w['procs']):
            for op in sorted(w['ops']):
                edges.append([p, w['name'], op])
    # lecturas por procedure (UsedBy)
    redges = []
    for s in ana['Summary']:
        if s['ObjectType'] == 'TABLE' and s['ObjectName'].lower() in {k.lower() for k in reads}:
            for p in (s['UsedBy'] or ['(aplicacion)']):
                redges.append([p, s['ObjectName'], 'R'])

    return dict(caso=caso, flujo=flujo, db=ana['Database'], ejec=ejec, llam=llam, calls=sorted(calls), ex=ex, writes=writes, reads=reads, edges=edges, redges=redges, mt=mt,
                ntrace=tr['TotalEvents'], nops=len(ana['Operations']))


def trig_text(t):
    return ', '.join('%s(%s)' % (x['Name'], x['Events'].replace(',', '/')) for x in t['Triggers']) if t else ''


def md_case(c, allw):
    L = []
    L.append('# %s — resumen' % c['caso'])
    L.append('')
    L.append('_Generado desde analysis_input.json, trace.json y metadata.json (BD %s). Columnas = las que el caso **escribe** (INSERT/UPDATE)._' % c['db'])
    L.append('')
    pw = collections.defaultdict(lambda: collections.defaultdict(set))
    for t, w in c['writes'].items():
        for p in w['procs']:
            pw[p][w['name']] |= w['ops']
    L.append('## Procedures')
    L.append('')
    L.append('**Ejecutados por la aplicación que escriben** (→ tablas)')
    L.append('')
    conesc = [p for p in pw if p != '(aplicacion)']
    for p in sorted(conesc, key=str.lower):
        quien = ('ejecutado ×%d' % c['ejec'][p]) if c['ejec'].get(p) else 'llamado por ' + ', '.join(sorted({a for a, b in c['calls'] if b == p})) if any(b == p for a, b in c['calls']) else 'anidado (sin EXEC visible)'
        L.append('- `%s` (%s) → %s' % (p, quien, '; '.join('%s [%s]' % (t, '/'.join(sorted(o))) for t, o in sorted(pw[p].items()))))
    if '(aplicacion)' in pw:
        L.append('- *(SQL de la aplicación, sin procedure)* → %s' % '; '.join('%s [%s]' % (t, '/'.join(sorted(o))) for t, o in sorted(pw['(aplicacion)'].items())))
    L.append('')
    solo = sorted([p for p in c['ejec'] if p not in pw], key=str.lower)
    L.append('**Ejecutados solo de lectura** (%d): %s' % (len(solo), ', '.join('`%s`' % p for p in solo) or '—'))
    L.append('')
    lsolo = sorted([p for p in c['llam'] if p not in pw and p not in c['ejec']], key=str.lower)
    if lsolo: L.append('**Llamados por otro procedure, solo lectura** (%d): %s' % (len(lsolo), ', '.join('`%s`' % p for p in lsolo))); L.append('')
    L.append('## Tablas escritas')
    L.append('')
    for t, w in sorted(c['writes'].items(), key=lambda x: x[1]['name'].lower()):
        mo = c['mt'].get(t)
        L.append('### %s — %s · por: %s%s' % (w['name'], ', '.join(sorted(w['ops'])), ', '.join(sorted(w['procs'])), (' · triggers: ' + trig_text(mo)) if mo and mo['Triggers'] else ''))
        mine = list(w['cols'].values())
        if w['nocols']: L.append('- ⚠ hay un INSERT sin lista de columnas (columnas exactas no determinables)')
        L.append('- **Escritas (%d):** %s' % (len(mine), ', '.join(mine) or '— (solo DELETE o INSERT sin columnas)'))
        if mo:
            allc = [x['Name'] for x in mo['Columns']]
            mine_l = set(w['cols'])
            otros = [x for x in allc if x.lower() not in mine_l and x.lower() in allw.get(t, set())]
            nunca = [x for x in allc if x.lower() not in mine_l and x.lower() not in allw.get(t, set())]
            if otros: L.append('- **Escritas solo en otros casos (%d):** %s' % (len(otros), ', '.join(otros)))
            if nunca and not w['nocols']: L.append('- **No escritas en ningún caso (%d):** %s' % (len(nunca), ', '.join(nunca)))
        else:
            L.append('- _tabla no incluida en metadata.json_')
        L.append('')
    L.append('## Tablas solo leídas (%d)' % len(c['reads']))
    L.append('')
    L.append(', '.join(sorted(c['reads'], key=str.lower)) or '—')
    L.append('')
    return '\n'.join(L)


def main():
    casos = sorted(d for d in os.listdir(BASE) if os.path.isfile(os.path.join(BASE, d, 'analysis_input.json')) and os.path.isfile(os.path.join(BASE, d, 'trace.json')))
    data = {k: parse_case(k) for k in casos}
    allw = collections.defaultdict(set)
    for c in data.values():
        for t, w in c['writes'].items(): allw[t] |= set(w['cols'])
    for k, c in data.items():
        open(os.path.join(BASE, k, 'resumen.md'), 'w', encoding='utf-8').write(md_case(c, allw))
        print(k, 'resumen.md', len(c['writes']), 'tablas escritas,', len(c['ejec']) + len(c['llam']), 'procedures')
    # nombres de procedures: un solo nombre por minusculas (distintos casos escriben mayusculas distintas)
    canon = {}
    def cn(n):
        return canon.setdefault(n.lower(), n)
    # datos para el visor
    out = {'generado': __import__('datetime').datetime.now().strftime('%Y-%m-%d %H:%M'), 'casos': {}}
    for k, c in data.items():
        out['casos'][k] = dict(
            pasos=(json.load(open(os.path.join(BASE, k, 'flujo.json'), encoding='utf-8')) if os.path.isfile(os.path.join(BASE, k, 'flujo.json')) else None),
            flujo=[[x[0], x[2], cn(x[3]), x[4], x[5], x[6]] for x in c['flujo']],
            edges=[[cn(a), b, o] for a, b, o in c['edges']], redges=[[cn(a), b, o] for a, b, o in c['redges']], calls=[[cn(a), cn(b)] for a, b in c['calls']],
            ejec={cn(k): v for k, v in c['ejec'].items()}, ex={cn(p): v for p, v in c['ex'].items()},
            cols={w['name']: list(w['cols'].values()) for w in c['writes'].values()},
            trig={w['name']: trig_text(c['mt'].get(t)) for t, w in c['writes'].items() if c['mt'].get(t) and c['mt'][t]['Triggers']})
    os.makedirs(VISOR, exist_ok=True)
    open(os.path.join(VISOR, 'datos.js'), 'w', encoding='utf-8').write('window.DATOS = ' + json.dumps(out, ensure_ascii=False, separators=(',', ':')) + ';')
    print('visor/datos.js', os.path.getsize(os.path.join(VISOR, 'datos.js')) // 1024, 'KB')


main()
