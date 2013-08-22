Imports System.IO

Public Enum NavigationDiscogsType
    release
    images
    updaterelease
End Enum
Public Class WindowsDiscogs
    Public Event UpdateSource(ByVal NouvelleSource As String)

    Private DiscogsInfos As Discogs = New Discogs
    Private MemIdRelease As String = ""
    Private Sub Fenetre_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
    End Sub
    'Reponse au message de fermeture de la fenetre
    Private Sub Bouton_Fermer_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Hide()
    End Sub
    Private Sub Fenetre_Closing(ByVal sender As Object, ByVal e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        'e.Cancel = True
        'Me.Hide()
    End Sub

    '*****************************************************************************************************************************
    '**********************************************FONCTIONS PUBLIQUES INTERFACAGE************************************************
    '*****************************************************************************************************************************
    Public Sub AfficheRelease(ByVal idrelease As String)
        If ((Visibility = Windows.Visibility.Hidden) Or (Not IsLoaded)) Then
            Owner = Application.Current.MainWindow
            Show()
        End If
        DiscogsRelease.Visibility = Windows.Visibility.Visible
        Titre.Content = "Informations Discogs"
        DiscogsRelease.UpdateRelease(idrelease, DiscogsInfos)
    End Sub

    'FONCTIONS UTILITES POUR LA GESTION DE LA FENETRE AFFICHAGE DES IMAGES
    '*****************************************************************************************************************************
    '**********************************************GESTION DE LA SOURIS***********************************************************
    '*****************************************************************************************************************************
    Dim StartPoint As Point
    Dim TypeObjetClic As Object
    Dim PlateformVista As Boolean
    Private Sub Fenetre_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        StartPoint = New Point
    End Sub
    Private Sub Fenetre_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(e.OriginalSource)
        TypeObjetClic = e.OriginalSource
    End Sub
    '  Dim FenetreReduite As Boolean = False
    Private Sub Fenetre_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseLeave
        StartPoint = New Point
        'Opacity = 0.8
        '       Me.BeginStoryboard(FindResource("AffichageProgressif"))
        '       FenetreReduite = True
    End Sub
    Private Sub Fenetre_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        'Opacity = 1
        If TypeOf (e.OriginalSource) Is Image And TypeOf (TypeObjetClic) Is Image Then
            If (InStr(CType(e.OriginalSource, Image).Name, "iDiscogs") <> 0) Then
                Dim MousePos As Point = e.GetPosition(e.OriginalSource)
                Dim Diff As Vector = StartPoint - MousePos
                If StartPoint.X <> 0 And StartPoint.Y <> 0 And ((e.LeftButton = MouseButtonState.Pressed) And (Math.Abs(Diff.X) > 2) Or
                                                                (Math.Abs(Diff.Y) > 2)) Then
                    Dim BitmapData As MemoryStream = Nothing
                    Dim ImageOriginale As Image = CType(e.OriginalSource, Image)
                    BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(
                                                       TagID3.tagID3Object.FonctionUtilite.UploadImage(ImageOriginale.Source))
                    If BitmapData Is Nothing Then Exit Sub
                    If PlateformVista Then
                        Dim TabParametres(0) As KeyValuePair(Of String, Object)
                        TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.Dib, BitmapData)
                        DragSourceHelper.DoDragDrop(e.OriginalSource, e.GetPosition(e.OriginalSource),
                                                    DragDropEffects.Copy, TabParametres)
                    Else
                        Dim data As DataObject = New DataObject()
                        data.SetData(DataFormats.Dib, BitmapData)
                        Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
                    End If
                    e.Handled = True
                End If
            End If
        End If
    End Sub

    '*****************************************************************************************************************************
    '**********************************************GESTION DU DRAG AND DROP*******************************************************
    '*****************************************************************************************************************************
    Private Sub Fenetre_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragEnter
        If PlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                        e.Data, e.GetPosition(e.OriginalSource),
                                                        e.Effects, "", "")
        e.Handled = True
    End Sub
    Private Sub Fenetre_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragLeave
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Private Sub Fenetre_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragOver
        e.Effects = DragDropEffects.None
        If PlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "", "")
        e.Handled = True
    End Sub

    '*****************************************************************************************************************************
    '**********************************************GESTION DE L'INTERFACE*********************************************************
    '*****************************************************************************************************************************
    Private Sub Entete_Fenetre_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Entete_Fenetre.MouseDown
        If e.ChangedButton = MouseButton.Left Then Me.DragMove()
    End Sub
    Private Sub Bouton_Transfert_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Bouton_Transfert.Click
        If DiscogsRelease.Visibility = Windows.Visibility.Visible Then RaiseEvent UpdateSource(DiscogsRelease.IdRelease)
    End Sub

End Class
