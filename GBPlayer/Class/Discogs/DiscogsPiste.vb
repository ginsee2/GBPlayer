
Public Class DiscogsPiste
    Inherits DiscogsObjet
    Public Property numPiste As String
    Public Property nomPiste As String
    Public Property dureePiste As String
    Public ReadOnly Property nomPisteConcat As String
        Get
            Return "<" & numPiste & "> " & nomPiste & " |" & dureePiste & "|"
        End Get
    End Property
End Class
