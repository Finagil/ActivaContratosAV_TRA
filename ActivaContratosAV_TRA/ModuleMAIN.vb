Module Activation_Module
    Dim UsuarioGlobalCorreo As String = "Activaciones@cmoderna.com"
    Dim DS As New ProdDS
    Sub Main()
        Console.WriteLine("Tradicionales")
        ActivaTRA()
        Console.WriteLine("Avios")
        ActivaMinistracionesAV()
    End Sub

    Sub ActivaTRA()
        Dim ta As New ProdDSTableAdapters.Vw_CXP_ContratosPagadosTableAdapter
        ta.Fill(DS.Vw_CXP_ContratosPagados)
        For Each r As ProdDS.Vw_CXP_ContratosPagadosRow In DS.Vw_CXP_ContratosPagados
            If r.Tipar = "F" Then
                Dim IvaCap As Decimal = ta.IvaTabla(r.noContrato)
                Dim IvaEq As Decimal = r.Ivaeq
                IvaEq = IvaEq - (IvaCap + r.IvaAmorin)
                If IvaEq <= -1 Or IvaEq >= 1 Then
                    MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", "Error en IVA Contrato", "El IVA del equipo no conicide con el IVA de la Tabla, Favor de notificar a CONTABILIDAD.")
                    Continue For
                End If
            End If
            ta.UpdateFechaPago(r.fechaPago.ToString("yyyyMMdd"), r.noContrato)
            ta.ActivaContrato(r.noContrato)
            If r.FechaActivacion.Trim.Length <> 8 Then
                ta.UpdateFechaActivacion(r.fechaPago.ToString("yyyyMMdd"), r.noContrato)
            End If
            CorreoConfirmacion(r)
        Next

    End Sub

    Sub CorreoConfirmacion(r As ProdDS.Vw_CXP_ContratosPagadosRow)
        Dim Asunto As String = "Ministración liberada por Tesoreria (" & r.Descr & ")  "
        Dim Mensaje As String
        Mensaje = "<table BORDER=1><tr><td><strong>Contrato</strong></td><td><strong>Cliente</strong></td><td><strong>Importe</strong></td><td><strong>Producto</strong></td></tr>"
        Mensaje += "<tr><td>" & r.noContrato & "</td>"
        Mensaje += "<td>" & r.Descr & "</td>"
        Mensaje += "<td ALIGN=RIGHT>" & r.MontoFinanciado.ToString("n2") & "</td>"
        Mensaje += "<td>" & r.TipoCredito & "</td>"
        Mensaje += "</tr>"
        Mensaje += "</table>"
        MandaCorreoFase(UsuarioGlobalCorreo, "MESA_CONTROL", Asunto, Mensaje)
        MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", Asunto, Mensaje)

        Dim taSegui As New ProdDSTableAdapters.Vw_CRED_SeguimientosTableAdapter
        Dim Rr As ProdDS.Vw_CRED_SeguimientosRow
        taSegui.Fill(DS.Vw_CRED_Seguimientos, r.Cliente)

        If DS.Vw_CRED_Seguimientos.Rows.Count > 0 Then
            Asunto = "Cliente con Seguimientos sin asignar (" & r.noContrato & ") "
            Mensaje += "<BR>Este Cliente tiene seguimientos sin asignación de contrato<BR>"
            For Each rr In DS.Vw_CRED_Seguimientos.Rows
                Mensaje += "<BR>" & Rr.Compromiso & "<BR>"
            Next
            MandaCorreoUser(UsuarioGlobalCorreo, Rr.Analista, Asunto, Mensaje)
            MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", Asunto, Mensaje)
        End If

    End Sub

    Private Sub ActivaMinistracionesAV()
        Dim ta As New ProdDSTableAdapters.AV_MinistracionesTableAdapter
        Dim taDet As New ProdDSTableAdapters.DetalleFINAGILTableAdapter
        Dim Tiie As New ProdDSTableAdapters.Vw_TIIEpromedioTableAdapter
        Dim diaAnterior As Date = Now.AddDays(-90)
        Dim cFechaPago As String = ""
        Dim nConsecutivo As Integer = 0
        Dim nSaldoFinal As Decimal = 0
        Dim nSaldoInicial As Decimal = 0
        Dim nTasaBP As Decimal = 0
        Dim FechaAplicacion As Date
        Dim tax As New ProdDSTableAdapters.MinistracionSinEdoCtaTableAdapter
        Dim tx As New ProdDS.MinistracionSinEdoCtaDataTable

        ta.TESO_ConfirmaMinistracionesCXP()
        ta.UpdateFechaPagoGastos(diaAnterior.ToString("yyyyMMdd"))
        FechaAplicacion = ta.SacaFechaAplicacion
        ta.Fill(DS.AV_Ministraciones, diaAnterior.ToString("yyyyMMdd"))

        For Each DR As ProdDS.AV_MinistracionesRow In DS.AV_Ministraciones
            Try
                If DR.FechaPago < FechaAplicacion.ToString("yyyyMM01") Then
                    cFechaPago = FechaAplicacion.ToString("yyyyMM01")
                Else
                    cFechaPago = DR.FechaPago
                End If
                taDet.Fill(DS.DetalleFINAGIL, DR.Anexo, DR.Ciclo)

                If DS.DetalleFINAGIL.Rows.Count = 0 Then
                    nConsecutivo = 1
                    nSaldoInicial = 0
                    nSaldoFinal = DR.Importe
                Else
                    For Each drD As ProdDS.DetalleFINAGILRow In DS.DetalleFINAGIL
                        nConsecutivo = drD("Consecutivo")
                        nSaldoInicial = drD("SaldoFinal")
                    Next
                    nConsecutivo += 1
                    nSaldoFinal = nSaldoInicial + DR.Importe
                End If

                If DR.Tipta = "7" Then
                    nTasaBP = Math.Round(DR.Tasas + DR.DiferencialFINAGIL, 4)
                Else
                    Tiie.Fill(DS.Vw_TIIEpromedio, Mid(DTOC(DateAdd(DateInterval.Month, -1, CTOD(cFechaPago))), 1, 6))
                    If DS.Vw_TIIEpromedio.Rows.Count > 0 Then
                        nTasaBP = DS.Vw_TIIEpromedio.Rows(0).Item(0)
                        nTasaBP = Math.Round(nTasaBP + DR.DiferencialFINAGIL, 4)
                    Else
                        nTasaBP = 0
                    End If
                End If
                nSaldoFinal = Math.Round(nSaldoFinal + DR.Fega + DR.Garantia, 2)
                taDet.Insert(DR.Anexo, DR.Ciclo, DR.Cliente, nConsecutivo, cFechaPago, cFechaPago, 0, nTasaBP, nSaldoInicial, DR.Documento, DR.Importe, DR.Fega, DR.Garantia, 0,
                             nSaldoFinal, Date.Now.Date, 0, 0, 0, "", "", 0)

                ta.UpdateMinistracion(DR.Anexo, DR.Ciclo, DR.Ministracion, DR.Documento)
                ta.ActivaAV(DR.Anexo, DR.Ciclo)
            Catch ex As Exception
                MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", "error DetalleFinagil", ex.Message)
            End Try
        Next
        Try
            tax.Fill(tx, Now.AddDays(-10).ToString("yyyyMMdd"))
            For Each rr As ProdDS.MinistracionSinEdoCtaRow In tx.Rows
                MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", "error DetalleFinagil", "No pasaron a estado de Cuenta: " & rr.Anexo & rr.Ciclo & rr.Consecutivo & rr.Concepto)
            Next
        Catch ex As Exception
            MandaCorreoFase(UsuarioGlobalCorreo, "SISTEMAS", "error DetalleFinagil", ex.Message)
        End Try
    End Sub

End Module
