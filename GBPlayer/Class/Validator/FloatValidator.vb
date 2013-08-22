Public Class ValidationFloat
    Inherits ValidationRule
    Public Overrides Function Validate(ByVal value As Object, ByVal cultureInfo As System.Globalization.CultureInfo) As System.Windows.Controls.ValidationResult
        Dim Texte As String = CType(value, String)
        Dim EchecValidation As Boolean
        For Each i In CType(value, String)
            If Not (Char.IsNumber(i) Or (i = ".")) Then
                EchecValidation = True
                Exit For
            End If
        Next
        If EchecValidation Then
            Return New ValidationResult(False, "Le format numérique n'est pas valide")
        Else
            Return New ValidationResult(True, Nothing)
        End If
    End Function
End Class