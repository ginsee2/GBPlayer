Imports System.Windows.Controls.Primitives
Imports System.Collections.ObjectModel
Imports System.IO

Public Class wpfFolderBrowser

    Private ScanDisque As wpfDrives
    Private SelectedPathEnCours As String
    Private _DiskCollection As New ObservableCollection(Of String)
    Public Property SelectedPath As String
        Get
            Return SelectedPathEnCours
        End Get
        Set(ByVal value As String)
            If _DiskCollection.Count > 0 Then
                For Each i In _DiskCollection
                    If Trim(ExtraitChaine(i, "", "\")) = Trim(ExtraitChaine(value, "", "\")) Then tagDrives.Text = i
                Next
                If tagDrives.Text = "" Then tagDrives.Text = _DiskCollection.First
                ListeRepertoires.gbRacine = Trim(ExtraitChaine(tagDrives.Text, "", "["))
                If ExtraitChaine(value, ":\", "", 2) <> "" Then
                    If Directory.Exists(value) Then
                        ListeRepertoires.gbFolderSelected = value
                    End If
                End If
            End If
        End Set
    End Property
    Public ReadOnly Property DiskCollection As ObservableCollection(Of String)
        Get
            Return _DiskCollection
        End Get
    End Property
    Public Property DialogTitle As String
        Get
            Return Titre.Content
        End Get
        Set(ByVal value As String)
            Titre.Content = value
        End Set
    End Property

    Public Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        ScanDisque = New wpfDrives(wpfDrives.EnumDriveUpdate.Null)
        ScanSystemDisk()
    End Sub
    Private Sub wpfFolderBrowser_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        If tagDrives.Text = "" Then
            tagDrives.Text = DiskCollection.First
            ListeRepertoires.gbRacine = Trim(ExtraitChaine(tagDrives.Text, "", "["))
        End If
    End Sub

    Private Sub CreateFolder_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)

    End Sub

    Private Sub Ok_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        SelectedPathEnCours = ListeRepertoires.gbFolderSelected
        Close()
    End Sub

    Private Sub Annuler_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Close()
    End Sub

    Private Sub ScanSystemDisk()
        Try
            Dim MemTexte = tagDrives.Text
            _DiskCollection.Clear()
            For Each i In ScanDisque.DiskCollection
                _DiskCollection.Add(i)
            Next
            For Each i In ScanDisque.DiskCollection
                If Trim(ExtraitChaine(i, "", "[")) = Trim(ExtraitChaine(MemTexte, "", "[")) Then tagDrives.Text = i
            Next
        Catch ex As Exception
            MsgBox("Erreur lors du scan des disques")
        End Try
    End Sub

    Private Sub tagDrives_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles tagDrives.SelectionChanged
        ListeRepertoires.gbRacine = Trim(ExtraitChaine(CType(e.AddedItems.Item(0), String), "", "["))
    End Sub
    Private Sub EnteteMsgbox_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles EnteteMsgbox.MouseDown
        If e.ChangedButton = MouseButton.Left Then Me.DragMove()
    End Sub
End Class
