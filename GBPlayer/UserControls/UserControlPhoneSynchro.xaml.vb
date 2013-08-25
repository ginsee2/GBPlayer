Imports System.IO
Imports System.Windows.Threading

Public Class UserControlPhoneSynchro
    Private phoneSynchroInitialised As Boolean
    Public Property DisplayValidation As Boolean

    Private Sub UserControlPhoneSynchro_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        RepertoiresSynchro.ForceCopyFiles = True
        RepertoiresSynchro.ExecuteDirectoryDropAction = AddressOf DirectoryDropCopyAction
        RepertoiresSynchro.ExecuteFileDropAction = AddressOf FileDropCopyAction
        ListeFichiersASynchro.DisableCustomisation = True
        ListeFichiersASynchro.ForceCopyFiles = True
        ListeFichiersASynchro.ExecuteDirectoryDropAction = AddressOf DirectoryDropCopyAction
        ListeFichiersASynchro.ExecuteFileDropAction = AddressOf FileDropCopyAction
    End Sub
    '**************************************************************************************************************
    '************************************INITIALISATION et FINALISATION DE LA LISTE********************************
    '**************************************************************************************************************
    Private Sub UserControlPhoneSynchro_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If Not DisplayValidation Then Exit Sub
        ListeFichiersASynchro.DisplayValidation = True
        If Not phoneSynchroInitialised Then
            RepertoiresSynchro.gbRacine = Application.Config.phoneSynchro_directory
            ListeFichiersASynchro.MiseAJourListeRepertoire(RepertoiresSynchro.gbRacine)
            RacineSynchro.Content = RepertoiresSynchro.gbRacine
            phoneSynchroInitialised = True
        End If
        Dim SauvegardeCollection As New Dictionary(Of String, GridViewColumn)
        For Each i In CType(ListeFichiersASynchro.ListeFichiers.View, GridView).Columns
            Select Case CType(i.Header, GridViewColumnHeader).Tag.ToString
                Case "Image"
                    i.CellTemplate = Me.FindResource("TemplateColonneImage")
                    SauvegardeCollection.Add(CType(i.Header, GridViewColumnHeader).Tag.ToString, i)
                Case "Nom"
                    i.CellTemplate = Me.FindResource("TemplateColonneArtiste")
                    i.Width = 380
                    SauvegardeCollection.Add(CType(i.Header, GridViewColumnHeader).Tag.ToString, i)
            End Select
        Next
        ListeFichiersASynchro.ListeFichiers.Style = Me.FindResource("GBListViewWithoutHeader")
        CType(ListeFichiersASynchro.ListeFichiers.View, GridView).Columns.Clear()
        For Each i In SauvegardeCollection
            CType(ListeFichiersASynchro.ListeFichiers.View, GridView).Columns.Add(i.Value)
        Next
    End Sub
    Private Sub UserControlPhoneSynchro_Unloaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Unloaded
        If Not DisplayValidation Then Exit Sub
        SaveConfiguration()
    End Sub
    Public Sub SaveConfiguration()
        If Not DisplayValidation Then Exit Sub
        Application.Config.phoneSynchro_directory = RepertoiresSynchro.gbRacine
        'ConfigPerso.UpdateListeColonnes(ConfigUtilisateur.SELLLIST_ListeColonnes, CType(XMLBinding.View, GridView).Columns)
        'If ColonneTriEnCours IsNot Nothing Then
        ' ConfigUtilisateur.SELLLIST_ColonneTriee = ColonneTriEnCours.Tag & ";" &
        ' IIf(IconeDeTriEnCours.Direction = ListSortDirection.Ascending, "A", "D")
        ' End If
        'Dim source As String = DataProvider.Source.LocalPath
        'DataProvider.Document.Save(source)
    End Sub

    Sub RepertoiresSynchro_SelectionChanged(ByVal Path As String) Handles RepertoiresSynchro.SelectionChanged
        ListeFichiersASynchro.MiseAJourListeRepertoire(Path)
    End Sub
    '**************************************************************************************************************
    '************************************ACTION SUR LES FICHIERS LORS DE LA COPIE**********************************
    '**************************************************************************************************************
    Private Sub DirectoryDropCopyAction(NomDirectory As String)
        Dim DirList As IEnumerable(Of String) = From file In Directory.EnumerateDirectories(NomDirectory)
        If DirList IsNot Nothing Then
            Array.ForEach(DirList.ToArray, Sub(i As String)
                                               DirectoryDropCopyAction(i)
                                           End Sub)
        End If
        Dim DirList2 As IEnumerable(Of String) = From file In Directory.EnumerateFiles(NomDirectory, "*.*")
        If DirList2 IsNot Nothing Then
            Array.ForEach(DirList2.ToArray, Sub(i As String)
                                                Me.Dispatcher.BeginInvoke(Sub(FichierATraiter As String)
                                                                              FileDropCopyAction(FichierATraiter)
                                                                          End Sub, DispatcherPriority.Background, {i})
                                            End Sub)
        End If
    End Sub
    Private Sub FileDropCopyAction(NomFichier As String)
        Using Fichier As New gbDev.TagID3.tagID3Object
            Fichier.SearchPadding = False
            Fichier.FileNameMp3 = NomFichier
            If Fichier.FileNameMp3 <> "" Then
                Fichier.ID3v1_Ok = True
                Fichier.ID3v1_Album = Fichier.ID3v2_Texte("TALB")
                Fichier.ID3v1_Annee = Fichier.ID3v2_Texte("TYER")
                Fichier.ID3v1_Artiste = Fichier.ID3v2_Texte("TPE1")
                Fichier.ID3v1_Genre = Fichier.ID3v2_Texte("TCON")
                Fichier.ID3v1_Titre = Fichier.ID3v2_Texte("TIT2")
                If Fichier.ID3v2_Ok Then
                    If Fichier.ID3v2_Texte("TALB") = "" Then
                        Fichier.ID3v2_Texte("TALB") = Fichier.ID3v2_Texte("TPE1") & " - " & Fichier.ID3v2_Texte("TIT2")
                    End If
                    Fichier.ID3v2_SetImage("", "Label")
                    Fichier.ID3v2_SetImage("", "Dos Pochette")
                End If
                If Fichier.ID3v2_EXTHEADER_PaddingSize > 0 Then Fichier.ID3v2_EXTHEADER_PaddingSize = 0
                Fichier.SaveID3()
            End If
        End Using
    End Sub
    '**************************************************************************************************************
    '*******************************************PARAMETRAGE DU DOSSIER DE SYNCHRO**********************************
    '**************************************************************************************************************
    Private Sub BPPathSynchro_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPPathSynchro.Click
        Dim dialog = New wpfFolderBrowser ' System.Windows.Forms.FolderBrowserDialog
        dialog.SelectedPath = RepertoiresSynchro.gbRacine
        dialog.DialogTitle = "Choix du dossier de synchronisation"
        dialog.ShowDialog()
        If dialog.SelectedPath <> "" Then
            RepertoiresSynchro.gbRacine = dialog.SelectedPath
            RacineSynchro.Content = RepertoiresSynchro.gbRacine
            Application.Config.phoneSynchro_directory = RepertoiresSynchro.gbRacine
        End If
    End Sub

End Class
