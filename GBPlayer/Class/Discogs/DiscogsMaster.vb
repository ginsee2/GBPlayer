
Public Class DiscogsMaster
    Inherits DiscogsBaseRelease
    Private _versions As List(Of DiscogsNomRelease)
    Public Property nbrVersions As Integer
    Public Property versions As List(Of DiscogsNomRelease)
        Get
            If _versions Is Nothing Then
                _versions = New List(Of DiscogsNomRelease)
                Dim page As Integer = 1
                Do
                    If Not parent.GetMasterReleases(Me, page) Then Exit Do
                    page += 1
                Loop While _versions.Count < nbrVersions
            End If
            Return _versions
        End Get
        Set(ByVal value As List(Of DiscogsNomRelease))
            _versions = value
        End Set
    End Property
    Public Sub New(ByVal _parent As Discogs)
        parent = _parent
    End Sub
End Class

