# caso_008 — resumen

_Generado desde analysis_input.json, trace.json y metadata.json (BD TEXTILESJOC_BK). Columnas = las que el caso **escribe** (INSERT/UPDATE)._

## Procedures

**Ejecutados por la aplicación que escriben** (→ tablas)

- `UP_ACT_STOCKSHILCRU` (ejecutado ×3) → LG_MOVISTKHILCRU_GUIAS [DELETE/UPDATE]; LG_ORDCOMPITEM [DELETE/INSERT/UPDATE]; LG_STOCKSHILCRU [INSERT/UPDATE]; lg_movistkhilcru [DELETE/INSERT/UPDATE]
- `UP_ACTUALIZA_STOCKS_ITEM` (ejecutado ×1) → LG_MOVISTK [INSERT/UPDATE]; lg_movistkitem [INSERT]; lg_stocksitem [UPDATE]
- `UP_LG_ADQUISICIONES` (ejecutado ×6) → LG_ADQUISICIONES [UPDATE]
- `UP_Lg_Movstk` (ejecutado ×36) → LG_CLAMOV [UPDATE]; LG_MOVISTK [INSERT/UPDATE]; Lg_MoviStk_Bitacora [INSERT]
- `UP_MAN_ORDCOMP` (ejecutado ×4) → LG_NUMORDCOMP [UPDATE]; LG_ORDCOMP [DELETE/INSERT/UPDATE]; LG_ORDCOMP_BITACORA [INSERT]
- `UP_MAN_ORDCOMPCAMBIOESTADO` (ejecutado ×2) → LG_ITEM [UPDATE]; LG_ORDCOMP [DELETE/INSERT/UPDATE]; LG_ORDCOMPITEM [DELETE/INSERT/UPDATE]; Lg_OrdComp_Cambio_Estado [INSERT]; Lg_OrdComp_Servicios_Confeccion_Comb_Talla [DELETE/INSERT]
- `UP_MAN_ORDCOMPITEMN` (ejecutado ×7) → LG_ORDCOMPITEM [DELETE/INSERT/UPDATE]; LG_ORDCOMPITEM_BITACORA [INSERT]; TJ_ORDTRA_ITEMS [UPDATE]; tx_ordtra [INSERT/UPDATE]
- `UP_Tx_OrdTra` (ejecutado ×1) → tg_tipordtra [UPDATE]; tx_ordtra [INSERT/UPDATE]

**Ejecutados solo de lectura** (30): `CN_OBTIENE_ANO_PERIOD_VIGENTE`, `Devuelve_Centro_Costo`, `SM_AYUDA_ITEMS_CON_STOCKS`, `SM_AYUDA_PRECIO_ULT_COMPRAS`, `sm_muestra_ordenes_compra_segun_tipomov_OK`, `TI_SEL_ORDCOMPITEM_TINTO`, `TJO_MUESTRA_OTS_TEJEDURIA_GRUPO`, `UP_AyudaHilCru`, `UP_Lg_MoviStkHilCru`, `UP_Lg_MovstkItem`, `UP_Lg_OrdCompItem`, `UP_PRT_LG_ORDCOMPITEMREQ`, `UP_SEL_BUSCA_GUIAS`, `UP_SEL_ORDCOMP`, `UP_SEL_ORDCOMPITEM`, `UP_SEL_ORDCOMPITEMREP`, `UP_SEL_RPTOCOMPRACAB`, `UP_VERIFICA_MOV_AVIO_SAL`, `USP_CENCOSTO_USUARIO`, `USP_CLASES_ORDEN`, `USP_Cond_Pago`, `USP_Condicion_Venta`, `USP_Descuento`, `USP_Docum_Ingreso`, `USP_ESTADO_ORDEN`, `USP_ESTADO_PRODUCTO_OC`, `USP_LUGAR_ENTRE_TEJE`, `USP_Moneda`, `USP_PROCESOS`, `USP_TIEMPO_VIDA_PRODUCTO_OC`

**Llamados por otro procedure, solo lectura** (8): `LG_ACTUALIZA_FECHA_CANTIDADES_ORDCOMPITEM_SEGUN_MOV`, `lg_genera_precios_items_servicios_por_oc`, `LG_SM_MUESTRA_ALMACEN_TRANS`, `SM_CALCULA_IMPORTE_DOLARES`, `sm_revisa_exceso_cantidad_entregada_oc`, `SM_VALIDA_EXISTENCIA_GUIA`, `SM_VALIDA_FECHA_MOVIMIENTO_LG_ALMACEN`, `Textilesjoc_bk`

## Tablas escritas

### LG_ADQUISICIONES — UPDATE · por: UP_LG_ADQUISICIONES
- **Escritas (3):** ID_ESTADO_PRODUCTO_OC, FEC_NUEVA_SOLICITUD, NUM_TIEMPO_VIDA_PROD
- _tabla no incluida en metadata.json_

### LG_CLAMOV — UPDATE · por: UP_Lg_Movstk
- **Escritas (1):** ULT_MOVIMIENTO
- _tabla no incluida en metadata.json_

### LG_ITEM — UPDATE · por: UP_MAN_ORDCOMPCAMBIOESTADO
- **Escritas (6):** Cod_Proveedor, Ser_OrdComp, Cod_OrdComp, Pre_UltComp, Fec_UltComp, Cod_MonUltComp
- _tabla no incluida en metadata.json_

### LG_MOVISTK — INSERT, UPDATE · por: UP_ACTUALIZA_STOCKS_ITEM, UP_Lg_Movstk
- **Escritas (30):** FlagAsignacionConcar, COD_ALMACEN, NUM_MOVSTK, FEC_CREACION, FEC_MOVSTK, COD_USUARIO, COD_PROVEEDOR, COD_CENCOST, ULTSEC, COD_TIPMOV, SER_ORDCOMP, COD_ORDCOMP, COD_CLIENTE, COD_TIPORDPRO, COD_ORDPRO, OBSERVACIONES, NUM_GUIA, COD_FABRICA, NUM_MOVSTK_2DA, NUM_SECORD, COD_TIPORDTRA, COD_ORDTRA, FLG_STATUS_ADMINISTRATIVO, VOUCH_CLIENTE_TEX, VOUCH_COLOR, VOUCH_ORDEN_PEDIDO, VOUCH_OBS_ADICI, COD_MAQUINA, COD_OPERARIO, COD_SUPERVISOR
- _tabla no incluida en metadata.json_

### Lg_MoviStk_Bitacora — INSERT · por: UP_Lg_Movstk
- **Escritas (14):** COD_ALMACEN, NUM_MOVSTK, FEC_MODIFICACION, COD_ORDTRA, COD_PROVEEDOR, COD_CENCOST, COD_TIPMOV, SER_ORDCOMP, COD_ORDCOMP, COD_CLIENTE, COD_USUARIO, NUM_GUIA, ACCION, num_requerimiento
- _tabla no incluida en metadata.json_

### lg_movistkhilcru — DELETE, INSERT, UPDATE · por: UP_ACT_STOCKSHILCRU
- **Escritas (23):** cod_almacen, num_movstk, num_secuencia, cod_hiltel, can_movimiento, ser_ordcomp, cod_ordcomp, sec_ordcomp, fec_movstk, cod_tipmov, cod_tipordpro, cod_ordpro, cod_tipordtra, cod_ordtra, cod_proveedor, cod_ordprov, num_bultos, observaciones, cod_calidad, cod_ordtra_tinto, num_secuencia_ordtra, conos, NUM_GUIA
- _tabla no incluida en metadata.json_

### LG_MOVISTKHILCRU_GUIAS — DELETE, UPDATE · por: UP_ACT_STOCKSHILCRU
- **Escritas (1):** Num_Secuencia
- _tabla no incluida en metadata.json_

### lg_movistkitem — INSERT · por: UP_ACTUALIZA_STOCKS_ITEM
- **Escritas (20):** cod_almacen, num_movstk, cod_item, cod_comb, cod_color, cod_talla, cod_destino, cod_estcli, ser_ordcomp, cod_ordcomp, fec_movstk, cod_tipmovi, can_movimiento, sec_ordcomp, num_secuencia, cod_tipordpro, Cod_Prov, cod_maquina_tejeduria, Peso_Kgs, cod_ordpro
- _tabla no incluida en metadata.json_

### LG_NUMORDCOMP — UPDATE · por: UP_MAN_ORDCOMP
- **Escritas (1):** ULT_ORDCOMP
- _tabla no incluida en metadata.json_

### LG_ORDCOMP — DELETE, INSERT, UPDATE · por: UP_MAN_ORDCOMP, UP_MAN_ORDCOMPCAMBIOESTADO
- **Escritas (31):** SER_ORDCOMP, COD_ORDCOMP, COD_PROVEEDOR, COD_CONDVENT, COD_DESCUENTO, PORC_IGV, COD_MONEDA, COD_LUGENTR, OBSERVACIONES, COD_STAORDCOMP, FEC_CREACION, COD_CLAORDCOMP, FEC_ENTREGA_INICIO, FEC_ENTREGA_FIN, ULT_SECORDCOMP, COD_CENCOST, COD_GRUPO, COD_PROTEX, FLG_TOTAL_PARCIAL, COD_CLIENTE, NUM_SOLICITUD, COD_PURORD, MOTIVO_SOLICITUD, COD_RESPONSABLE, cod_cliente_tex, ser_ordcomp_tex, cod_ordcomp_tex, Fec_1ra_Lib, Fec_Ult_Lib, Cod_Usuario, Imp_Total_Dol
- _tabla no incluida en metadata.json_

### LG_ORDCOMP_BITACORA — INSERT · por: UP_MAN_ORDCOMP
- **Escritas (6):** SER_ORDCOMP, COD_ORDCOMP, COD_PROVEEDOR, FECHA, ACCION, COD_USUARIO
- _tabla no incluida en metadata.json_

### Lg_OrdComp_Cambio_Estado — INSERT · por: UP_MAN_ORDCOMPCAMBIOESTADO
- ⚠ hay un INSERT sin lista de columnas (columnas exactas no determinables)
- **Escritas (0):** — (solo DELETE o INSERT sin columnas)
- _tabla no incluida en metadata.json_

### Lg_OrdComp_Servicios_Confeccion_Comb_Talla — DELETE, INSERT · por: UP_MAN_ORDCOMPCAMBIOESTADO
- ⚠ hay un INSERT sin lista de columnas (columnas exactas no determinables)
- **Escritas (0):** — (solo DELETE o INSERT sin columnas)
- _tabla no incluida en metadata.json_

### LG_ORDCOMPITEM — DELETE, INSERT, UPDATE · por: UP_ACT_STOCKSHILCRU, UP_MAN_ORDCOMPCAMBIOESTADO, UP_MAN_ORDCOMPITEMN
- **Escritas (28):** Ser_OrdComp, Cod_OrdComp, Sec_OrdComp, Cod_Item, Cod_Comb, Cod_Color, Cod_Talla, Cod_Prov, Cod_Destino, Cod_EstCli, Cod_Descuento, Porc_IGV, Fec_Entrega_Inicio, Fec_Entrega_Fin, Pre_Unitario, Can_Requerida, Can_Comprada, Can_Recibida, Cod_ItemProv, Fac_EquiProv, Observaciones, Kgs_En_OrdTra_Tejeduria, Flg_Status_Tejeduria, Cod_Hilado, COD_FABRICA, COD_ORDPRO, FEC_1RA_ENTREGA, FEC_ULT_ENTREGA
- _tabla no incluida en metadata.json_

### LG_ORDCOMPITEM_BITACORA — INSERT · por: UP_MAN_ORDCOMPITEMN
- **Escritas (10):** Ser_OrdComp, Cod_OrdComp, Sec_OrdComp, FECHA, Cod_Item, Cod_Color, Pre_Unitario, Can_Comprada, Accion, Cod_Usuario
- _tabla no incluida en metadata.json_

### LG_STOCKSHILCRU — INSERT, UPDATE · por: UP_ACT_STOCKSHILCRU
- **Escritas (14):** cod_almacen, cod_tipordtra, cod_ordtra, cod_hiltel, cod_calidad, can_entradas, can_salidas, flg_status, cod_ordprov, cod_proveedor, FEC_1ER_INGRESO, FEC_ULT_INGRESO, bultos_entrada, conos_entrada
- _tabla no incluida en metadata.json_

### lg_stocksitem — UPDATE · por: UP_ACTUALIZA_STOCKS_ITEM
- **Escritas (3):** can_stock, fec_1er_salida, fec_ult_salida
- _tabla no incluida en metadata.json_

### tg_tipordtra — UPDATE · por: UP_Tx_OrdTra
- **Escritas (1):** correlativo
- _tabla no incluida en metadata.json_

### TJ_ORDTRA_ITEMS — UPDATE · por: UP_MAN_ORDCOMPITEMN
- **Escritas (1):** KGS_PROGR
- _tabla no incluida en metadata.json_

### tx_ordtra — INSERT, UPDATE · por: UP_MAN_ORDCOMPITEMN, UP_Tx_OrdTra
- **Escritas (12):** KGS_PROGR, Cod_TipOrdTra, Cod_Ordtra, Cod_Proveedor, Cod_OrdProv, Flg_Status, Observacion, Cod_GrupoTex, FEC_CREACION, Ser_OrdComp, Cod_OrdComp, Nivel_Costeo
- _tabla no incluida en metadata.json_

## Tablas solo leídas (62)

ALMACEN_CENTRO_COSTO, CC_MOTIVOS_DESAPROBACION_RECEPCION_PROVEEDOR, CN_CONTROL_ADICIONAL, CN_TIPOCAMBIO, CN_TIPOSDOCUM, ES_GRUPOLOG, ES_GRUPOTEX, IT_HILADO, LB_COLOR, LG_ALMACEN, LG_CLAORDCOMP, LG_CONDVENT, LG_CONTROL_TRANSFERENCIAS, LG_COTIZACIONES, LG_DSCTOS, LG_ESTADO_PRODUCTO_OC, LG_FAMITE, LG_ITEMCOMB, LG_ITEMMED, LG_ITEMPROV, LG_LUGENTR, LG_MODORDCOMP, LG_MOVISTKTELCRU, LG_ORDCOMP_PROCESOS_ADICIONALES, LG_ORDCOMPITEMREQ, LG_PROVEEDOR, LG_SEGORDCOMP, LG_STAORDCOMP, LG_TIEMPO_VIDA_PRODUCTO_OC, LG_TIP_CONDPAGO, LG_TIPMOVIALM, LG_TIPOSMOV, LG_TIPREQ, SEGURIDAD, SPT_INDEXES_VIEW, SPT_PRIMARY_KEYS_VIEW, TG_CENCOSTO, TG_CLIENTE, TG_CONTROL, TG_DESTINO, TG_ESTCLI, TG_FABRICA, TG_IGV, TG_MONEDA, TG_ORDCOMPNOTA, TG_TALLA, TH_ORDTRA_ITEMS, TI_ORDTRA_TINTORERIA_ITEMS, TI_SEG_ACESSO_PRECIOS, TI_TIPO_ORDEN_TINTORERIA, TJ_ORDTRA_HILADOS, TX_CLIENTE, TX_MAQUINAS_TEJEDURIA, TX_ORDCOMP, TX_ORDCOMPITEM_TINTO, TX_PROCESOS, TX_TELA, TX_TELACOMB, TX_TELAMED, TX_TIPOSORDTRA, USUARIO_CENCOST, VALIDACION_TIPOS_MOV_CAMPOS
