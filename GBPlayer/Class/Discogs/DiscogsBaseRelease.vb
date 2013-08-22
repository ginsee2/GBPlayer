Option Compare Text

Public Class DiscogsBaseRelease
    Inherits DiscogsNomRelease
    Public Property artiste As New DiscogsNomArtiste
    Public Property style As String
    Public Property genre As String
    Public Property pistes As New List(Of DiscogsPiste) '<NumeroPiste> NomPiste |Duree|'
    Public Property images As New List(Of DiscogsImage) 'adresseImage (hauteur,largeur) adresseimage150
End Class
