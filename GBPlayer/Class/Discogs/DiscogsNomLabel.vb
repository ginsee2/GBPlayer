
Public Class DiscogsNomLabel
    Inherits DiscogsObjet
    Public Property id As String
    Public Property nom As String
    Public Property catalogue As String
    Public Sub New(ByVal _nom As String)
        nom = _nom
    End Sub
End Class
