
Public Class DiscogsCompositeur
    Inherits DiscogsObjet
    Public Property numPiste As String
    Public Property nomPiste As String
    Public Property nomCompositeurs As String
    Public ReadOnly Property nomCompositeursConcat As String
        Get
            Return "<" & numPiste & " - " & nomPiste & "> " & nomCompositeurs
        End Get
    End Property
End Class
