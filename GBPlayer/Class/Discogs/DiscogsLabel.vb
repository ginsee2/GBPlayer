
Public Class DiscogsLabel
    Inherits DiscogsNomLabel
    Private _releases As List(Of DiscogsNomRelease)
    Public Property profile As String
    Public Property infoContact As String
    Public Property urls As New List(Of String)
    Public Property labelParent As DiscogsNomLabel
    Public Property labelEnfants As New List(Of DiscogsNomLabel)
    Public Property images As New List(Of DiscogsImage)
    Public Property nbrReleases As Integer
    Public Property releases As List(Of DiscogsNomRelease)
        Get
            If _releases Is Nothing Then
                _releases = New List(Of DiscogsNomRelease)
                Dim page As Integer = 1
                Do
                    If Not parent.GetLabelReleases(Me, page) Then Exit Do
                    page += 1
                Loop While _releases.Count < nbrReleases
            End If
            Return _releases
        End Get
        Set(ByVal value As List(Of DiscogsNomRelease))
            _releases = value
        End Set
    End Property
    Public Sub New(ByVal _parent As Discogs)
        MyBase.new("")
        parent = _parent
    End Sub
End Class
