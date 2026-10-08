# caso_004 — resumen

_Generado desde analysis_input.json, trace.json y metadata.json (BD TEXTILESJOC_BK). Columnas = las que el caso **escribe** (INSERT/UPDATE)._

## Procedures

**Ejecutados por la aplicación que escriben** (→ tablas)

- `TI_QYC_UP_MAN_LG_MOVISTKITEM` (ejecutado ×3) → Ct_Errores_Costos [DELETE/INSERT]; LG_MOVISTKITEM_COSTO [UPDATE]; Lg_MoviStkItem [INSERT/UPDATE]; Lg_MoviStkItem_Bitacora [INSERT]; Lg_OrdCompItem [UPDATE]; Lg_StocksItem [UPDATE]; Ti_OrdTra_Tintoreria_Qyc [DELETE/UPDATE]; Ti_OrdTra_Tintoreria_Relacionadas [UPDATE]; Ti_Ordtra_Tintoreria_Procesos [UPDATE]
- *(SQL de la aplicación, sin procedure)* → LG_INGRESO_TIPOS_GUIAS [UPDATE]; Lg_MoviStk [DELETE/INSERT/UPDATE]; Lg_MoviStk_Bitacora [INSERT]

**Ejecutados solo de lectura** (18): `CN_OBTIENE_ANO_PERIOD_VIGENTE`, `DEVUELVE_MOTIVOS_DESASIGNADOS`, `DEVUELVE_VALIDACION_PESO`, `Lg_Seg_Usuarios_Qyc`, `TI_AYUDA_ORDENES_COMPRA_QYC`, `TI_AYUDA_PROVEEDORES_QYC`, `ti_muestra_maquinas_propia`, `Ti_Muestra_OperarioAcabados_Todos`, `TI_MUESTRA_PARTIDAS_POR_ASIGNAR_RECETA`, `TI_MUESTRA_QYC_PARTIDA`, `TI_MUESTRA_TI_ORDTRA_TINTORERIA_PROCESOS`, `TI_ORDTRA_RESERVAS_CRUDO_CLIENTES`, `TI_QYC_MOVIMIENTOS_CABECERA_MUESTRA`, `TI_QYC_MUESTRA_DETALLE_MOVIMIENTO`, `ti_sm_muestra_partidas_principal`, `ti_sm_muestra_telas_partida_proceso_v2`, `TI_UP_MAN_TI_ORDTRA_TINTORERIA_PROCESOS`, `UP_REPSTOCKFAM_QYC`

**Llamados por otro procedure, solo lectura** (2): `CALCULA_COSTOS_QUIMICOS_MOVSTK_KARDEX`, `LG_ACTUALIZA_FECHA_CANTIDADES_ORDCOMPITEM_SEGUN_MOV`

## Tablas escritas

### Ct_Errores_Costos — DELETE, INSERT · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (6):** cod_almacen, ano, mes, num_movstk, num_secuencia, cod_error_costos

### LG_INGRESO_TIPOS_GUIAS — UPDATE · por: (aplicacion)
- **Escritas (1):** Flg_Asignado
- **No escritas en ningún caso (7):** Guia, Cod_Proveedor, CodTip_Guia, Fecha, Usuario, Abre_Cliente, Referencia

### Lg_MoviStk — DELETE, INSERT, UPDATE · por: (aplicacion) · triggers: TR_LG_MOVISTK_FlagAsignacion(INSERT/UPDATE), TRG_LG_MOVISTK_VALIDAR_FLAG_ASIGNACION(INSERT/UPDATE)
- **Escritas (23):** FlagAsignacionConcar, COD_ALMACEN, NUM_MOVSTK, COD_PROVEEDOR, SER_ORDCOMP, COD_ORDCOMP, COD_CENCOST, NUM_GUIA, COD_ORDTRA, COD_USUARIO, OBSERVACIONES, COD_TIPMOV, COD_TIPORDTRA, FEC_MOVSTK, FEC_CREACION, COD_CLIENTE, FLG_RE, Num_sec_Reproceso, Num_Requerimiento, COD_ALMACEN_ORIGEN, NUM_MOVSTK_ORIGEN, COD_ALMACEN_DESTINO, NUM_MOVSTK_DESTINO
- **Escritas solo en otros casos (14):** UltSec, Cod_TipOrdpRO, Cod_Ordpro, Cod_Fabrica, Num_MovStk_2da, num_secord, Flg_status_administrativo, VOUCH_CLIENTE_TEX, VOUCH_COLOR, VOUCH_ORDEN_PEDIDO, VOUCH_OBS_ADICI, COD_MAQUINA, COD_OPERARIO, COD_SUPERVISOR
- **No escritas en ningún caso (39):** Flg_StatusVAL, Ser_docum, Num_Docum, Usuario_valorizo, Nom_transportista, Dom_transportista, ruc_transportista, Ser_Guia_Propia, Nro_Guia_Propia, Num_Placa, num_corre, importe_segun_oc, Sec_Transportista, TEMPACT, Ser_Parte_Salida, Numero_Parte_Salida, Num_MovStk_3ra, Cod_AlmacenRel, Num_MovStkRel, cod_almacen_rel, num_movstk_rel, Glosa_Hilado, Cod_MotTra, TIP_TRABAJADOR, COD_TRABAJADOR, NRO_CONOS_HILOS_COSER, NUM_CORRE_VENTAS, flg_status_facturacion, SER_DOCUM_VENTAS, NUM_DOCUM_VENTAS, cod_area_produccion, Cod_Equipo, FEC_EMISION_VOUCHER, VOUCH_HILO, VOUCH_HILO_LOTE, VOUCH_HILO_GUIA, VOUCH_OF, SerDocumConcar, NumDocumConcar

### Lg_MoviStk_Bitacora — INSERT · por: (aplicacion)
- **Escritas (14):** Cod_Almacen, Num_MovStk, Fec_Modificacion, COD_ORDTRA, Cod_Proveedor, Cod_CenCost, Cod_TipMov, Ser_OrdComp, Cod_OrdComp, Cod_Cliente, Cod_Usuario, Num_Guia, Num_Requerimiento, Accion

### Lg_MoviStkItem — INSERT, UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (29):** COD_ALMACEN, NUM_MOVSTK, NUM_SECUENCIA, CAN_MOVIMIENTO, SER_ORDCOMP, COD_ORDCOMP, SEC_ORDCOMP, FEC_MOVSTK, COD_TIPMOVI, COD_TIPORDPRO, COD_ORDPRO, COD_ITEM, COD_ESTCLI, secu_requerimiento, Cod_Proceso, Secuencia, Sec_Receta, Cod_Prov, COSTO_UNITARIO_ORDCOMP_SOL, COSTO_UNITARIO_ORDCOMP_DOL, Can_Kardex, FLG_VALORIZADO, IMP_FACTURA, imp_valorizado, imp_valorizado_dolares, PRECIO_UNITARIO_SOL, PRECIO_UNITARIO_DOL, COSTO_UNITARIO_SOL, COSTO_UNITARIO_DOL
- **Escritas solo en otros casos (6):** Cod_Comb, Cod_Color, Cod_Talla, Cod_Destino, Cod_Maquina_Tejeduria, Peso_Kgs
- **No escritas en ningún caso (15):** Fec_Creacion, Imp_Factura_Dolares, Precio_FOB_Importacion, Valor_Import_Dolares, Valor_Import_Soles, flg_status_aprobacion_recepcion_cc_proveedor, Fecha_Revision_recepcion_cc_proveedor, Cod_Desaprobacion_CC_Proveedor, Can_Movimiento_Desaprobada, Aql_Revision, Observacion_Revision_Recepcion_CC_Proveedor, Cod_Uusario_Revision_Recepcion_CC_Proveedor, precio_valorizacion, Cod_CenCost_Contable, FLG_ACTUALIZA_KARDEX_MANUAL

### Lg_MoviStkItem_Bitacora — INSERT · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (10):** Cod_Almacen, Num_MovStk, Num_Secuencia, Fec_Creaccion, Cod_Item, Can_Movimiento, Cod_Prov, Cod_Usuario, PC, ACCION

### LG_MOVISTKITEM_COSTO — UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (10):** COD_ALMACEN, ULT_NUM_MOVSTK, ULT_NUM_SECUENCIA, CAN_KARDEX, COSTO_VAL_SOL, COSTO_VAL_DOL, IMP_FACT_SOL, IMP_FACT_DOL, FEC_MODI, USUA_MODI
- **No escritas en ningún caso (4):** COD_ITEM, Cod_Prov, COMENTARIOS, FLG_VALIDADO_SIST

### Lg_OrdCompItem — UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (3):** CAN_RECIBIDA, FEC_1RA_ENTREGA, FEC_ULT_ENTREGA
- **Escritas solo en otros casos (25):** Ser_OrdComp, Cod_OrdComp, Sec_OrdComp, Cod_Item, Cod_Comb, Cod_Color, Cod_Talla, Cod_Destino, Cod_EstCli, Cod_Descuento, Porc_IGV, Fec_Entrega_Inicio, Fec_Entrega_Fin, Pre_Unitario, Can_Requerida, Can_Comprada, Fac_EquiProv, Cod_ItemProv, Observaciones, Cod_Prov, Kgs_En_OrdTra_Tejeduria, Flg_Status_Tejeduria, Cod_Hilado, COD_FABRICA, COD_ORDPRO
- **No escritas en ningún caso (13):** Fec_Cierre, Cod_Ordtra_Tejeduria, CAN_RECIBIDA_2DAUNIMED, Can_Requerida_2daunimed, Can_Comprada_2daunimed, Kgs_Requeridos, Flg_Status_Dua, Cantidad_Asignada_NP, Num_Secuencia_OT_Tejeduria, Flg_Incluido_Precio_Cotizacion, FLG_INCLUYE_IGV, PRE_UNITARIO_INCLUYE_IGV, Num_Guia

### Lg_StocksItem — UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (9):** can_stock, fec_1er_entrada, fec_ult_entrada, fec_1er_salida, fec_ult_salida, VALORIZADO_SOL, VALORIZADO_DOL, COSTO_UNIT_VALORIZADO_SOL, COSTO_UNIT_VALORIZADO_DOL
- **No escritas en ningún caso (15):** Cod_Almacen, Cod_Item, Cod_Comb, Cod_Color, Cod_Talla, Cod_destino, Cod_EstCli, Cod_Prov, Ubicacion_Fisica, CAN_COMPROMETIDA, FEC_PRIMERCOMPREMETIDA, FEC_ULTCOMPREMETIDA, FLG_STATUS_LABORATORIO, FLG_VALORIZA_OK, FLG_ACTUALIZA_KARDEX_MANUAL_STOCKS

### Ti_Ordtra_Tintoreria_Procesos — UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (1):** flg_Reproceso
- **Escritas solo en otros casos (23):** Cod_Ordtra, Cod_Proceso_Tinto, Secuencia, Fecha_Inicio, Fecha_Fin, Cod_Maquina_Tinto, Observaciones, cod_motivo_reproceso, Kilos, Tiempo_Estandar, SubSecuencia, Temperatura, Ancho, Densidad, Velocidad, Cod_Turno, cod_motivo_reproceso_Calidad, Cod_Operario, Cod_Supervisor, SEC_REPROCESO, FEC_REGISTRO, TIP_TRABAJADOR_OPERA, TIP_TRABAJADOR_SUPER
- **No escritas en ningún caso (10):** Num_Secuencia, Modo, Fecha_Fin_Automatico, Minutos, FEC_ULT_MODIFICACION, FEC_RECEPCION_RECETA, OBS_RECEPCION_RECETA, FLG_PASE_GESTION, FLG_TIENE_SUB_PROCESOS, FLG_TIENE_SUB_PROCESOS_REPRO

### Ti_OrdTra_Tintoreria_Qyc — DELETE, UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (3):** Cons_Requerido, Cod_Prov, cons_atendido
- **Escritas solo en otros casos (3):** Cod_OrdTra, Cod_Item, Cons_Acabado
- **No escritas en ningún caso (2):** Cons_Adicional, Cons_Reproce

### Ti_OrdTra_Tintoreria_Relacionadas — UPDATE · por: TI_QYC_UP_MAN_LG_MOVISTKITEM
- **Escritas (3):** Num_Movstk_Principal, Cod_Almacen, Num_movstk
- **No escritas en ningún caso (3):** Cod_OrdTra, Cod_OrdTra_Relacionada, Fec_Creacion

## Tablas solo leídas (75)

CN_CONTROL_ADICIONAL, CN_DOCUM, CN_TIPOCAMBIO, FT_MOVIMIENTOS_AFECTOS_QUIMICOS_VALORIZADO, HI_TURNO, IT_HILADO, LB_COLOR, LB_COLOR_ORINGAL_RGB, LG_ALMACEN, LG_DESASIGNADO_MOTIVO, LG_FAMGRUITE, LG_FAMITE, LG_ITEM, LG_MOVISTKHILCRU, LG_MOVISTKITEM_REQUERIMIENTO, LG_MOVISTKTELCRU, LG_ORDCOMP, LG_PROVEEDOR, LG_SEG_MOV_QUIMICOS, LG_TIPMOVIALM, LG_TIPMOVREL, LG_TIPOSMOV, PESO_ACTIVO_QUIMICOS, TG_CLIENTE, TG_CONTROL, TG_OPERARIO, TG_TIPANC, TI_AUTORIZADO_STOCKS_QYC_VALORIZADOS, TI_CURVATENIDO, TI_MAQUINAS_TINTO, TI_MOTIVO_REQ, TI_MOTIVOS_REPROCESO, TI_ORDTRA_RESERVAS_CRUDO, TI_ORDTRA_TINTORERIA, TI_ORDTRA_TINTORERIA_ITEMS, TI_ORDTRA_TINTORERIA_ITEMS_CRUDO_GUIAS, TI_ORDTRA_TINTORERIA_PROCESOS_BARRAS, TI_ORDTRA_TINTORERIA_PROCESOS_ITEMS, TI_ORDTRA_TINTORERIA_REPROCESOS_QYC, TI_ORDTRA_TINTORERIA_REQ_ADIC, TI_ORDTRA_TINTORERIA_REQ_ADIC_ITEMS, TI_PROCESOS_TINTORERIA, TI_PROCESOS_TINTORERIA_PARTIDA, TI_RECETAS_TINTORERIA, TI_RECETAS_TINTORERIA_DETALLE, TI_REQUERIMIENTOS_RECETA, TI_REQUERIMIENTOS_RECETA_COMREPROCESO, TI_STATUS_ORDENES_TINTORERIA, TI_SUBPROCESOS_TINTORERIA, TI_TINTORERIA_PROCESOOPERARIO, TJ_ORDTRA_HILOS_LOTES, TJ_ORDTRA_TEJEDURIA_ROLLOS, TX_ALMACEN, TX_CENCOSTO, TX_CLIENTE, TX_CONTROL, TX_MOTIVO_RECHAZO, TX_MOVISTK_TELA_TENIDA, TX_ORDCOMP_TELA_ARTICULO, TX_ORDCOMP_TELA_ESTRUCTURA, TX_ORDCOMP_TELA_LYCRA, TX_ORDCOMP_TELA_PORC_COMPOSICION, TX_ORDCOMP_TELA_TIPO, TX_ORDCOMP_TELA_TIPO_HILO, TX_ORDCOMP_TELA_TIPO_HILO_3, TX_ORDCOMP_TELA_TIPO_HILO_4, TX_ORDCOMP_TELA_TIPO_HILO_FLOTE, TX_ORDCOMP_TELA_TITULO1_CARA, TX_ORDCOMP_TELA_TITULO2_FLOTE, TX_ORDCOMP_TELA_TITULO3, TX_ORDCOMP_TELA_TITULO4, TX_ORDCOMPITEM_TINTO, TX_TELA, TX_TELACOMB, TX_TITULOS
