
Public Class DiscogsRelease
    Inherits DiscogsBaseRelease
    Public Property idMaster As String
    Public Property notes As String
    Public Property compositeurs As New List(Of DiscogsCompositeur) '<NumeroPiste - NomPiste> Compositeurs'
End Class
