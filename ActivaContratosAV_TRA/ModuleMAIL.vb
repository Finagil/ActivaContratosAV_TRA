Module ModuleMAIL
    Public Function MandaCorreoFase(De As String, Fase As String, Asunto As String, Mensaje As String, Optional ByVal Archivo As String = "") As Boolean
        Asunto = Asunto.Trim
        Dim taCorreos As New ProdDSTableAdapters.GEN_Correos_SistemaFinagilTableAdapter
        Dim users As New ProdDSTableAdapters.GEN_CorreosFasesTableAdapter
        Dim tu As New ProdDS.GEN_CorreosFasesDataTable
        Dim r As ProdDS.GEN_CorreosFasesRow
        AjustaCorreo(Asunto, Mensaje)
        MandaCorreoFase = False
        users.Fill(tu, Fase)
        For Each r In tu.Rows
            taCorreos.Insert(De, r.Correo, Asunto, Mensaje, False, Date.Now, Archivo)
            MandaCorreoFase = True
        Next
        taCorreos.Dispose()
        Return MandaCorreoFase
    End Function

    Public Sub MandaCorreoUser(De As String, Usuario As String, Asunto As String, Mensaje As String, Optional Archivo As String = "")
        Dim taCorreos As New ProdDSTableAdapters.GEN_Correos_SistemaFinagilTableAdapter
        Dim users As New SeguridadDSTableAdapters.UsuariosFinagilTableAdapter
        Dim tu As New SeguridadDS.UsuariosFinagilDataTable
        Dim r As SeguridadDS.UsuariosFinagilRow
        AjustaCorreo(Asunto, Mensaje)
        If InStr(Usuario, "@") > 0 Then
            taCorreos.Insert(De, Usuario, Asunto, Mensaje, False, Date.Now, Archivo)
        Else
            users.FillByUsuario(tu, Usuario)
            For Each r In tu.Rows
                taCorreos.Insert(De, r.correo, Asunto, Mensaje, False, Date.Now, Archivo)
            Next
        End If
        taCorreos.Dispose()
    End Sub

    Sub AjustaCorreo(ByRef Asunto As String, ByRef Mensaje As String)
        If Asunto.Length > 100 Then
            Asunto = Mid(Asunto, 1, 100)
        End If
        If Mensaje.Length > 2000 Then
            Mensaje = Mid(Mensaje, 1, 2000)
        End If
    End Sub

    Public Function CTOD(ByVal cFecha As String) As Date
        Dim nDia, nMes, nYear As Integer
        nDia = Val(Right(cFecha, 2))
        nMes = Val(Mid(cFecha, 5, 2))
        nYear = Val(Left(cFecha, 4))
        CTOD = DateSerial(nYear, nMes, nDia)
    End Function

    Public Function DTOC(ByVal dFecha As Date) As String
        Dim cDia, cMes, cYear, sFecha As String
        sFecha = dFecha.ToShortDateString
        cDia = Left(sFecha, 2)
        cMes = Mid(sFecha, 4, 2)
        cYear = Right(sFecha, 4)
        DTOC = cYear & cMes & cDia
    End Function

End Module
