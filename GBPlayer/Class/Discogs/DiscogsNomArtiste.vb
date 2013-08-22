
Public Class DiscogsNomArtiste
    Inherits DiscogsObjet
    Private _nom As String
    Public Property nom As String
        Get
            If anv <> "" Then Return anv Else Return _nom
        End Get
        Set(ByVal value As String)
            _nom = value
        End Set
    End Property
    Public Property anv As String
    Public Property id As String
End Class
