Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/07/10 rev 04/08/10
'DESCRIPTION :Classe d'affichage détails des tag ID3v2
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Windows.Controls.Primitives
Imports System.IO
Imports System.Windows.Interop
Imports System.Windows.Media.Animation
Imports System.Threading
Imports System.Net
Imports System.Text
Imports System.Xml
Imports System.Collections.ObjectModel
Imports System.Reflection
Imports System.Windows.Media.Imaging
Imports Newtonsoft.Json.Linq

'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class UserControlTagEditor
    Inherits UserControl
    Implements iNotifyShellUpdate

    Private Const GBAU_NOMDOSSIER_LISTESPERSO = "GBDev\GBPlayer\Data"
    Private Const GBAU_NOMFICHIER_LISTESPERSO = "DataModeleListesPerso.xml"
    Private Const GBAU_NOMRESSOURCE = "gbDev.DataModeleListesPerso.xml"
    Private Const GBAU_VERSIONLISTESPERSO = "1.0.1"
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS
    Public Event RequeteWebBrowser(ByVal url As Uri)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String)
    Public Event RequeteMenuContextuel(ByVal NomChamp As String, ByRef MenuContextuelLocal As ContextMenu, ByVal FonctionReponse As RoutedEventHandler)
    Public Event RequeteInfosDiscogs(ByVal id As String, ByVal TypeInfos As NavigationDiscogsType)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DE LA FENETRE

    Dim ListeDesFichiersEnCours As New List(Of String)
    Friend InfosFichier As tagID3FilesInfosDO
    Dim DiscogsInfos As New Discogs
    Dim WithEvents ShellWatcher As New System.IO.FileSystemWatcher
    Private PathFichierListesPerso As String
    Dim WithEvents TimerDiscogs As Timers.Timer = New Timers.Timer
    Private FenetreInfo As WindowUpdateProgress

    Private _ListeStyles As New ObservableCollection(Of String)
    Public ReadOnly Property ListeStyles As ObservableCollection(Of String)
        Get
            Return _ListeStyles
        End Get
    End Property
    Public Property BibliothequeLiee As tagID3Bibliotheque Implements iNotifyShellUpdate.BibliothequeLiee
    Public Property Filter As String Implements iNotifyShellUpdate.Filter

    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New()
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        StyleDataProvider.Source = tagID3FilesInfosDO.GetDataProvider
    End Sub
    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

    Private Sub gbEditeurTAG_GiveFeedback(ByVal sender As Object, ByVal e As System.Windows.GiveFeedbackEventArgs) Handles Me.GiveFeedback
        Debug.Print("passage")
    End Sub
    Private Sub clsEditeurTAG_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        'Dim Config As ConfigStorage = New ConfigStorage
        'Config = ConfigStorage.LoadConfig
        If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
            If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        End If
        Dim Infos As tagID3FilesInfos = New tagID3FilesInfos("")
        InfosFichier = CType(FindResource("InfosFichier"), tagID3FilesInfosDO)
        InfosFichier.Update(Infos)
        ' Me.Top = Config.PlayerPosition.Y
        ' Me.Left = Config.PlayerPosition.X
    End Sub
    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    Sub ClosePlayer()
    End Sub
    '**************************************************************************************************************
    '*****************************GESTION DE LA MISE A JOUR DES INFORMATIONS DES FICHIERS**************************
    '**************************************************************************************************************
    Dim DemandeMaJ As Long
    Public Sub MiseAJourListeFichiers(ByVal ListeFichiers As List(Of String), ByVal Fenetre As MainWindow)
        DemandeMaJ += 1
        Dim ReadTag As New SelectionChangedDelegate(AddressOf ListeFichiersMp3SelectionChanged)
        ReadTag.BeginInvoke(ListeFichiers, Fenetre, DemandeMaJ, Nothing, Nothing)
    End Sub
    Private Delegate Sub SelectionChangedDelegate(ByVal ListeFichiers As System.Collections.Generic.List(Of String), ByVal Fenetre As MainWindow, ByVal NumMiseAJour As Long)
    Private Delegate Sub MiseAJourAffichageDelegate(ByVal Infos As tagID3FilesInfos, ByVal Fenetre As MainWindow, ByVal NumMaJ As Long)
    Private Sub ListeFichiersMp3SelectionChanged(ByVal ListeFichiers As System.Collections.Generic.List(Of String), ByVal Fenetre As MainWindow, ByVal NumMiseAJour As Long)
        Dim InitListeOk As Boolean
        Dim Infos As tagID3FilesInfos = Nothing
        Dim ExtensionEnCours As String = ""
        ListeDesFichiersEnCours = ListeFichiers
        For Each i As String In ListeDesFichiersEnCours
            If NumMiseAJour <> DemandeMaJ Then Exit Sub
            If Not InitListeOk Then
                ExtensionEnCours = Path.GetExtension(i)
                Infos = New tagID3FilesInfos(i)
                If Infos.Taille = 0 Then
                    Infos = Nothing
                    Exit For
                End If
                InitListeOk = True
            Else
                If Path.GetExtension(i) <> ExtensionEnCours Then
                    Infos = Nothing
                    Exit For
                Else
                    Infos.Add(i)
                End If
            End If
        Next
        If NumMiseAJour = DemandeMaJ Then _
            Me.Dispatcher.BeginInvoke(New MiseAJourAffichageDelegate(AddressOf ListeFichiersMiseAJourAffichage),
                                        System.Windows.Threading.DispatcherPriority.Loaded, {Infos, Fenetre, NumMiseAJour})
    End Sub
    Private Sub ListeFichiersMiseAJourAffichage(ByVal Infos As tagID3FilesInfos, ByVal Fenetre As MainWindow, ByVal NumMaJ As Long)
        If NumMaJ = DemandeMaJ Then
            If ((Infos Is Nothing) Or (ListeDesFichiersEnCours.Count = 0)) Then
                Infos = New tagID3FilesInfos("")
                Me.IsEnabled = False
            ElseIf Infos IsNot Nothing Then
                If (Infos.Extension <> "mp3") Then
                    Me.IsEnabled = False
                Else
                    Me.IsEnabled = True
                End If
            End If
            InfosFichier = CType(FindResource("InfosFichier"), tagID3FilesInfosDO)
            InfosFichier.Update(Infos, Fenetre)
            If InfosFichier.Nom IsNot Nothing And InfosFichier.Nom <> "*" Then
                ShellWatcher.IncludeSubdirectories = True
                ShellWatcher.Path = Fenetre.ListeRepertoires.gbRacine ' Path.GetDirectoryName(InfosFichier.NomComplet)
                ShellWatcher.Filter = Path.GetFileName("*.mp3")
                ShellWatcher.NotifyFilter = NotifyFilters.LastWrite Or NotifyFilters.FileName
                ShellWatcher.EnableRaisingEvents = True 'A desactiver à la fin de la lecture
            End If
            InfosFichier.Selected = True
            TimerDiscogs.Stop()
            TimerDiscogs.Interval = 500
            TimerDiscogs.Start()
        End If
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************
    Dim StartPoint As Point
    Dim PointDepart As Point
    Dim PointArrivee As Point
    Dim Largeur As Double
    Dim TypeObjetClic As Object
    Dim PlateformVista As Boolean
    Dim DessinEditionEnCours As Boolean
    Private Sub EditeurTAG_PreviewMouseLeftButtonUp(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonUp
        StartPoint = New Point
        If DessinEditionEnCours Then
            If GrilleAffichageImages.IsMouseCaptured Then
                GrilleAffichageImages.ReleaseMouseCapture()
            End If
            DessinEditionEnCours = False
        End If
    End Sub
    Private Sub EditeurTAG_PreviewMouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseLeftButtonDown
        StartPoint = e.GetPosition(tagImage)
        TypeObjetClic = e.OriginalSource
        If EditionImageEnCours Then
            If wpfApplication.FindAncestor(e.OriginalSource, "Grid") IsNot Nothing Then
                If CType(wpfApplication.FindAncestor(e.OriginalSource, "Grid"), Grid).Name = "GrilleAffichageImages" Then
                    PointDepart = e.GetPosition(ZoneDessin)
                    GrilleAffichageImages.CaptureMouse()
                    DessinEditionEnCours = True
                End If
            End If
        End If
    End Sub
    Private Sub EditeurTAG_MouseLeave(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseLeave
        StartPoint = New Point
    End Sub
    Private Sub EditeurTAG_MouseMove(ByVal sender As Object, ByVal e As System.Windows.Input.MouseEventArgs) Handles Me.MouseMove
        If EditImageEnCours.IsChecked Then
            If (TypeOf (e.OriginalSource) Is Grid) Then
                If ((CType(e.OriginalSource, Grid).Name) = "GrilleAffichageImages") Then
                    If DessinEditionEnCours Then
                        If (DessinEditionEnCours) Then
                            PointArrivee.X = e.GetPosition(ZoneDessin).X
                            PointArrivee.Y = e.GetPosition(ZoneDessin).Y
                            If (PointArrivee.X < 0) Then PointArrivee.X = 0
                            If (PointArrivee.X > ZoneDessin.ActualWidth) Then PointArrivee.X = ZoneDessin.ActualWidth
                            If (PointArrivee.Y < 0) Then PointArrivee.Y = 0
                            If (PointArrivee.Y > ZoneDessin.ActualWidth) Then PointArrivee.Y = ZoneDessin.ActualWidth
                            Dim Rect As New RectangleGeometry(New Rect(New Point(PointDepart.X, PointDepart.Y), New Point(PointArrivee.X, PointArrivee.Y)))
                            Dim cir As New EllipseGeometry(New Rect(New Point((PointArrivee.X - PointDepart.X) * 2, (PointArrivee.X - PointDepart.X) * 2)))
                            cir.Center = PointDepart
                            Dim aLargeur As Double = cir.Bounds.Width
                            If Not EditCarreEnCours.IsChecked Then
                                If ((aLargeur / 2 + PointDepart.X) > ZoneDessin.ActualWidth) Or
                                    ((PointDepart.X - aLargeur / 2) < 0) Or
                                    ((aLargeur / 2 + PointDepart.Y) > ZoneDessin.ActualHeight) Or
                                    ((PointDepart.Y - aLargeur / 2) < 0) Then
                                    Exit Sub
                                Else
                                    Largeur = aLargeur
                                End If
                            End If
                            Dim cir2 As New EllipseGeometry(New Rect(New Point(10, 10)))
                            cir2.Center = PointDepart
                            ZoneDessin.Children.Clear()
                            If Not EditCarreEnCours.IsChecked Then
                                Dim Mypath As System.Windows.Shapes.Path = New System.Windows.Shapes.Path
                                Mypath.Stroke = Brushes.Orange
                                Mypath.StrokeThickness = 2
                                Mypath.Data = cir
                                ZoneDessin.Children.Add(Mypath)
                                Dim Mypath2 As System.Windows.Shapes.Path = New System.Windows.Shapes.Path
                                Mypath2.Stroke = Brushes.Red
                                Mypath2.StrokeThickness = 2
                                Mypath2.Data = cir2
                                ZoneDessin.Children.Add(Mypath2)
                            Else
                                Dim Mypath3 As System.Windows.Shapes.Path = New System.Windows.Shapes.Path
                                Mypath3.Stroke = Brushes.Blue
                                Mypath3.StrokeThickness = 2
                                Mypath3.Data = Rect
                                ZoneDessin.Children.Add(Mypath3)
                            End If
                        End If
                    End If
                End If
            End If
        Else
            If InfosFichier IsNot Nothing Then
                If InfosFichier._NomOrigine <> "" Then
                    If TypeOf (e.OriginalSource) Is Image And TypeOf (TypeObjetClic) Is Image Then
                        Dim DonneeSurvolee As tagID3FilesInfosDO = InfosFichier
                        If ((CType(e.OriginalSource, Image).Name) = "tagImage") Or
                           ((CType(e.OriginalSource, Image).Name) = "tagImageLabel") Or
                            ((CType(e.OriginalSource, Image).Name) = "tagImageDos") Then
                            Dim MousePos As Point = e.GetPosition(tagImage)
                            Dim Diff As Vector = StartPoint - MousePos
                            If StartPoint.X <> 0 And StartPoint.Y <> 0 And ((e.LeftButton = MouseButtonState.Pressed) And (Math.Abs(Diff.X) > 2) Or
                                                                            (Math.Abs(Diff.Y) > 2)) Then
                                Dim BitmapData As MemoryStream = Nothing
                                Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(DonneeSurvolee.NomComplet)
                                    Select Case (CType(e.OriginalSource, Image).Name)
                                        Case "tagImage"
                                            BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(NewTagId3.ID3v2_ImageData)
                                        Case "tagImageLabel"
                                            BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(NewTagId3.ID3v2_ImageData("Label"))
                                        Case "tagImageDos"
                                            BitmapData = TagID3.tagID3Object.FonctionUtilite.ConvertJpegdataToDibstream(NewTagId3.ID3v2_ImageData("Dos pochette"))
                                    End Select
                                    If BitmapData Is Nothing Then Exit Sub
                                End Using
                                Dim ImageOriginale As Image = CType(e.OriginalSource, Image)
                                If PlateformVista Then
                                    Dim TabParametres(0) As KeyValuePair(Of String, Object)
                                    TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.Dib, BitmapData)
                                    DragSourceHelper.DoDragDrop(ImageOriginale, e.GetPosition(e.OriginalSource),
                                                                DragDropEffects.Copy, TabParametres)
                                Else
                                    Dim data As DataObject = New DataObject()
                                    data.SetData(DataFormats.Dib, BitmapData)
                                    Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
                                End If
                                e.Handled = True
                            End If
                        End If
                    ElseIf (TypeOf (e.OriginalSource) Is Border) And (TypeOf (TypeObjetClic) Is Border) Then
                        If CType(e.OriginalSource, Border).Name = "EnteteEditeur" Then
                            Dim MousePos As Point = e.GetPosition(EnteteEditeur)
                            Dim Diff As Vector = StartPoint - MousePos
                            If StartPoint.X <> 0 And StartPoint.Y <> 0 And (e.LeftButton = MouseButtonState.Pressed And Math.Abs(Diff.X) > 2 Or
                                                                            Math.Abs(Diff.Y) > 2) Then
                                Dim FilesDataObject(0) As String
                                Dim FilesTextName As String = ""
                                FilesDataObject(0) = InfosFichier.NomComplet
                                FilesTextName = InfosFichier.Nom
                                If PlateformVista Then
                                    Dim TabParametres(1) As KeyValuePair(Of String, Object)
                                    TabParametres(0) = New KeyValuePair(Of String, Object)(DataFormats.FileDrop, FilesDataObject)
                                    TabParametres(1) = New KeyValuePair(Of String, Object)(DataFormats.Text, FilesTextName)
                                    DragSourceHelper.DoDragDrop(Me, e.GetPosition(Me), DragDropEffects.Copy, TabParametres)
                                Else
                                    Dim data As DataObject = New DataObject()
                                    data.SetData(DataFormats.FileDrop, FilesDataObject)
                                    data.SetData(DataFormats.Text, FilesTextName)
                                    Dim effects As DragDropEffects = DragDrop.DoDragDrop(Me, data, DragDropEffects.Copy)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If
    End Sub

    Dim ActionCopierEnCours As Boolean
    Dim ActionCopierTagEnCours As Boolean
    Private Sub EditeurTAG_Drop(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.Drop
        Dim DonneeSurvolee As tagID3FilesInfosDO = InfosFichier
        If (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
            FileDrop(e)
        ElseIf (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            If (DonneeSurvolee IsNot Nothing) Then
                e.Effects = e.AllowedEffects And DragDropEffects.Copy
                If PlateformVista Then DropTargetHelper.Drop(e.Data, e.GetPosition(e.OriginalSource), e.Effects)
                Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
                If Path.GetExtension(Origine) = ".mp3" Then
                    TagId3Drop(e)
                ElseIf Path.GetExtension(Origine) = ".jpg" Or Path.GetExtension(Origine) = ".jpeg" Then
                    FileDrop(e)
                End If
            Else
                e.Effects = DragDropEffects.None
            End If
        Else
            e.Effects = DragDropEffects.None
        End If
        e.Handled = True
    End Sub
    Private Sub EditeurTAG_DragEnter(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragEnter
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        DonneeSurvolee = InfosFichier
        If (e.Data.GetDataPresent(DataFormats.FileDrop)) Then
            'INSERER TEST DE TYPE FICHIER
            If (DonneeSurvolee IsNot Nothing) Then
                Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
                If Path.GetExtension(Origine) = ".mp3" Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Copy
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Copier les TAGID3 vers %1", DonneeSurvolee.Nom)
                ElseIf Path.GetExtension(Origine) = ".jpg" Then
                    e.Effects = e.AllowedEffects And DragDropEffects.Copy
                    If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                    e.Data, e.GetPosition(e.OriginalSource),
                                                                    e.Effects, "Copier l'image vers %1", DonneeSurvolee.Nom)

                End If
            Else
                e.Effects = DragDropEffects.None
                If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
            End If
            'e.Handled = True
        ElseIf (e.Data.GetDataPresent(DataFormats.Dib)) And (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "Copier l'image vers %1", DonneeSurvolee.Nom)
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                            e.Data, e.GetPosition(e.OriginalSource),
                                                            e.Effects, "", "")
            e.Handled = True
        End If
    End Sub
    Private Sub EditeurTAG_DragLeave(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragLeave
        If PlateformVista Then
            DropTargetHelper.DragLeave(e.Data)
        End If
        e.Handled = True
    End Sub
    Private Sub EditeurTAG_DragOver(ByVal sender As Object, ByVal e As System.Windows.DragEventArgs) Handles Me.DragOver
        Dim FlagPlateformVista As Boolean
        If (e.Data.GetDataPresent("DragSourceHelperFlags")) Then FlagPlateformVista = True Else FlagPlateformVista = False
        Dim DonneeSurvolee As tagID3FilesInfosDO = Nothing
        DonneeSurvolee = InfosFichier
        If ((e.Data.GetDataPresent(DataFormats.FileDrop)) Or (e.Data.GetDataPresent(DataFormats.Dib))) And
            (DonneeSurvolee IsNot Nothing) Then
            e.Effects = e.AllowedEffects And DragDropEffects.Copy
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragOver(e.GetPosition(e.OriginalSource), e.Effects)
        Else
            e.Effects = DragDropEffects.None
            If PlateformVista And FlagPlateformVista Then DropTargetHelper.DragEnter(Window.GetWindow(e.OriginalSource),
                                                                e.Data, e.GetPosition(e.OriginalSource),
                                                                e.Effects, "", "")
        End If
        'Me.BringIntoView()
        e.Handled = True
    End Sub

    'FONCTION DE TRAITEMENT DU DROP SUR LA LISTE
    Private Delegate Sub NoArgDelegate()
    Private Delegate Sub DelegateTacheCopieImage(ByVal ListeFichiers As List(Of String), ByVal NomFichierImage As String, ByVal TypeImage As String)
    Private Delegate Sub DelegateTacheCopieDataImage(ByVal ListeFichiers As List(Of String), ByVal ImageData As Byte(), ByVal TypeImage As String)
    Private Delegate Sub UpdateWindowsDelegate(ByVal NomFichier As String)
    Private NumProcess As Long
    Private Sub UpdateWindows(ByVal NomFichier As String)
        If InStr(NomFichier, "#INIT#") Then
            NumProcess = FenetreInfo.AddNewProcess(CInt(ExtraitChaine(NomFichier, "#INIT#", "", 6)))
        ElseIf NomFichier = "#END#" Then
            FenetreInfo.StopProcess(NumProcess)
        Else
            FenetreInfo.UpdateWindows(NomFichier, NumProcess)
        End If
    End Sub
    Private Sub FileDrop(ByVal e As System.Windows.DragEventArgs)
        Dim Formats As String() = e.Data.GetFormats()
        Dim Chemin As String = ""
        Dim NomDirectory As String = ""
        Dim NomFichier As String = ""
        If FenetreInfo Is Nothing Then FenetreInfo = CType(Application.Current.MainWindow, MainWindow).ProcessMiseAJour
        If e.Data.GetDataPresent(DataFormats.Dib) Then
            If TypeOf (e.OriginalSource) Is Image Then
                Dim TacheCopie As New DelegateTacheCopieDataImage(AddressOf TacheCopieDataImage)
                TacheCopie.BeginInvoke(InfosFichier.ListeFichiers, TagID3.tagID3Object.FonctionUtilite.ConvertDibstreamToJpegdata(
                                                                CType(e.Data.GetData(DataFormats.Dib), System.IO.MemoryStream)),
                                                                CType(e.OriginalSource, Image).Name, Nothing, Nothing)
                For Each i As String In InfosFichier.ListeFichiers
                    Select Case (CType(e.OriginalSource, Image).Name)
                        Case "tagImage", "tagSwitchImageDefaut"
                            SwitchImageDefautSelect.IsChecked = True
                        Case "tagImageLabel", "tagSwitchImageLabel"
                            SwitchImageLabelSelect.IsChecked = True
                        Case "tagImageDos", "tagSwitchImageDos"
                            SwitchImageDosSelect.IsChecked = True
                    End Select
                Next
            End If
        ElseIf e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
            If TypeOf (e.OriginalSource) Is Image Then
                Dim TacheCopie As New DelegateTacheCopieImage(AddressOf TacheCopieImage)
                TacheCopie.BeginInvoke(InfosFichier.ListeFichiers, Origine, CType(e.OriginalSource, Image).Name, Nothing, Nothing)
                For Each i As String In InfosFichier.ListeFichiers
                    Select Case (CType(e.OriginalSource, Image).Name)
                        Case "tagImage", "tagSwitchImageDefaut"
                            SwitchImageDefautSelect.IsChecked = True
                        Case "tagImageLabel", "tagSwitchImageLabel"
                            SwitchImageLabelSelect.IsChecked = True
                        Case "tagImageDos", "tagSwitchImageDos"
                            SwitchImageDosSelect.IsChecked = True
                    End Select
                Next
            End If
        End If
    End Sub
    Private Sub TacheCopieImage(ByVal ListeFichiers As List(Of String), ByVal NomFichierImage As String, ByVal TypeImage As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New UpdateWindowsDelegate(AddressOf UpdateWindows), "#INIT#" & ListeFichiers.Count)
        For Each i As String In ListeFichiers
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                      New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                         Path.GetFileName(i))
            Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(i, True)
                Select Case TypeImage
                    Case "tagImage", "tagSwitchImageDefaut"
                        NewTagId3.ID3v2_SetImage(NomFichierImage, "")
                    Case "tagImageLabel", "tagSwitchImageLabel"
                        NewTagId3.ID3v2_SetImage(NomFichierImage, "Label")
                    Case "tagImageDos", "tagSwitchImageDos"
                        NewTagId3.ID3v2_SetImage(NomFichierImage, "Dos pochette")
                End Select
            End Using
        Next
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
    End Sub
    Private Sub TacheCopieDataImage(ByVal ListeFichiers As List(Of String), ByVal ImageData As Byte(), ByVal TypeImage As String)
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New UpdateWindowsDelegate(AddressOf UpdateWindows), "#INIT#" & ListeFichiers.Count)
        For Each i As String In ListeFichiers
            Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                     New UpdateWindowsDelegate(AddressOf UpdateWindows),
                                        Path.GetFileName(i))
            Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(i, True)
                Select Case TypeImage
                    Case "tagImage", "tagSwitchImageDefaut"
                        NewTagId3.ID3v2_SetImage(ImageData, "")
                    Case "tagImageLabel", "tagSwitchImageLabel"
                        NewTagId3.ID3v2_SetImage(ImageData, "Label")
                    Case "tagImageDos", "tagSwitchImageDos"
                        NewTagId3.ID3v2_SetImage(ImageData, "Dos pochette")
                End Select
            End Using
        Next
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                                  New UpdateWindowsDelegate(AddressOf UpdateWindows), "#END#")
    End Sub
    'FONCTION DE TRAITEMENT DU DROP TAG SUR LA LISTE
    Private Sub TagId3Drop(ByVal e As System.Windows.DragEventArgs)
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            Dim Origine As String = CType(e.Data.GetData(DataFormats.FileDrop), String())(0)
            Dim TagId3Origine As TagID3.tagID3Object = New TagID3.tagID3Object(Origine)
            For Each i As String In InfosFichier.ListeFichiers
                Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(i, True)
                    Dim NomArtiste As String = NewTagId3.ID3v2_Texte("TPE1")
                    Dim NomTitre As String = NewTagId3.ID3v2_Texte("TIT2")
                    If TagId3Origine.FileNameMp3 IsNot Nothing Then NewTagId3.ID3v2_Frames = TagId3Origine.ID3v2_Frames
                    If NomArtiste <> "" Then NewTagId3.ID3v2_Texte("TPE1") = NomArtiste
                    If NomTitre <> "" Then NewTagId3.ID3v2_Texte("TIT2") = NomTitre
                End Using
            Next
        End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE L'INTERFACE DU CONTROLE----------------------------
    '***********************************************************************************************
    '   REPONSE AU DOULE CLICK SUR DIVERS ELEMENTS
    Private Sub tagPageWeb_MouseDoubleClick(ByVal sender As System.Object, ByVal e As System.Windows.Input.MouseButtonEventArgs)
        If tagPageWeb.Text <> "*" And tagPageWeb.Text <> "" Then
            Try
                Dim NewURI As Uri = New Uri(tagPageWeb.Text)
                RaiseEvent RequeteWebBrowser(NewURI)
                e.Handled = True
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Lien URL non valide", "Le lien """ & tagPageWeb.Text & """ est non valide", Nothing)
                'MsgBox("Le lien """ & tagPageWeb.Text & """ est non valide", MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Lien URL non valide")
            End Try
        End If
    End Sub
    Private Sub gbEditeurTAG_MouseDoubleClick(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.MouseDoubleClick
        Dim Newurl As String = ""
        If TypeOf (e.OriginalSource) Is Image Then
            If CType(e.OriginalSource, Image).Name = "tagImage" Then
                If tagPageWeb.Text <> "*" And tagPageWeb.Text <> "" Then
                    Try
                        Newurl = "http://www.discogs.com/viewimages?release=" & ExtraitChaine(tagPageWeb.Text, "/release", "", 9)
                        Dim NewURI As Uri = New Uri(Newurl)
                        RaiseEvent RequeteWebBrowser(NewURI)
                        e.Handled = True
                    Catch ex As Exception
                        wpfMsgBox.MsgBoxInfo("Lien URL non valide", "Le lien """ & Newurl & """ est non valide", Nothing)
                        '  MsgBox("Le lien """ & Newurl & """ est non valide", MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Lien URL non valide")
                    End Try
                End If
            End If
        End If
    End Sub

    Private Sub BPDiscogs_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim RequeteDiscogs As Discogs = New Discogs(Discogs.Get_ReleaseId(tagPageWeb.Text))
        If RequeteDiscogs.release.id IsNot Nothing Then
            tagLabel.Text = RequeteDiscogs.release.label.nom
            tagCatalogue.Text = RequeteDiscogs.release.label.catalogue
            tagAnnee.Text = RequeteDiscogs.release.annee
            tagStyle.Text = RequeteDiscogs.release.style
            InfosFichier.idRelease = RequeteDiscogs.release.id
        End If
    End Sub

    '   GESTION DE L'EDITION DE L'IMAGE
    Private EditionImageEnCours As Boolean
    Private ImageKeyBoardFocus As Boolean = False
    Private Sub EditImageEnCours_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If (SwitchImageDefautSelect.IsChecked And (InfosFichier.Image Is Nothing)) Or
            (SwitchImageLabelSelect.IsChecked And (InfosFichier.ImageLabel Is Nothing)) Or
            (SwitchImageDosSelect.IsChecked And (InfosFichier.ImageDosPochette Is Nothing)) Then EditImageEnCours.IsChecked = False
        If EditImageEnCours.IsChecked Then
            EditionImageEnCours = True
        Else
            If EditionImageEnCours Then
                AnnulationEditionImage()
            End If
        End If
    End Sub
    Private Sub ValidationEditImage_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim cbm As New CroppedBitmap
        Dim ImageEncours As Image = Nothing
        If False Then 'Largeur = 0 Then
            wpfMsgBox.MsgBoxInfo("Edition image", "Faire une sélection avant de valider le redimensionnement", ZoneDessin)
        Else
            Try
                ' If (PointDepart.X>PointArrivee.X)
                If SwitchImageDefautSelect.IsChecked Then
                    ImageEncours = tagImage
                ElseIf SwitchImageLabelSelect.IsChecked Then
                    ImageEncours = tagImageLabel
                ElseIf SwitchImageDosSelect.IsChecked Then
                    ImageEncours = tagImageDos
                End If
                Dim RapportImageX = 600 / ImageEncours.ActualWidth
                Dim RapportImageY = 600 / ImageEncours.ActualHeight
                If Not EditCarreEnCours.IsChecked Then
                    cbm = New CroppedBitmap(DirectCast(ImageEncours.Source, BitmapSource),
                                            New Int32Rect((PointDepart.X - (Largeur / 2)) * RapportImageX,
                                                            (PointDepart.Y - (Largeur / 2)) * RapportImageY,
                                                            Largeur * RapportImageX, Largeur * RapportImageX))
                Else
                    cbm = New CroppedBitmap(DirectCast(ImageEncours.Source, BitmapSource),
                                            New Int32Rect(PointDepart.X * RapportImageX,
                                                          PointDepart.Y * RapportImageY,
                                                          (PointArrivee.X - PointDepart.X) * RapportImageX,
                                                          (PointArrivee.Y - PointDepart.Y) * RapportImageX))
                End If
                'TagID3.tagID3Object.FonctionUtilite.SaveImage(TagID3.tagID3Object.FonctionUtilite.UploadImage(TagID3.tagID3Object.FonctionUtilite.CreateResizedImage(cbm, 600, 600)), "d:\essai.jpg")
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Erreur édition image", ex.Message, ZoneDessin)
                AnnulationEditionImage()
                Exit Sub
            End Try
            Using NewTagId3 As TagID3.tagID3Object = New TagID3.tagID3Object(InfosFichier.NomComplet, True)
                If SwitchImageDefautSelect.IsChecked Then
                    NewTagId3.ID3v2_SetImage(TagID3.tagID3Object.FonctionUtilite.UploadImage(
                                            TagID3.tagID3Object.FonctionUtilite.CreateResizedImage(cbm, 600, 600)), "")
                ElseIf SwitchImageLabelSelect.IsChecked Then
                    NewTagId3.ID3v2_SetImage(TagID3.tagID3Object.FonctionUtilite.UploadImage(
                                             TagID3.tagID3Object.FonctionUtilite.CreateResizedImage(cbm, 600, 600)), "Label")
                ElseIf SwitchImageDosSelect.IsChecked Then
                    NewTagId3.ID3v2_SetImage(TagID3.tagID3Object.FonctionUtilite.UploadImage(
                                             TagID3.tagID3Object.FonctionUtilite.CreateResizedImage(cbm, 600, 600)), "Dos pochette")
                End If
            End Using
        End If
        AnnulationEditionImage()
    End Sub
    Private Sub AnnulationEditionImage()
        ImageKeyBoardFocus = False
        'If EditionImageEnCours Then
        EditImageEnCours.IsChecked = False
        EditionImageEnCours = False
        tagImage.ReleaseMouseCapture()
        DessinEditionEnCours = False
        ZoneDessin.Children.Clear()
        Largeur = 0
        'End If
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DES MENUS CONTEXTUELS---------------------------------
    '***********************************************************************************************
    Private Async Sub gbEditeurTAG_PreviewMouseRightButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles Me.PreviewMouseRightButtonDown
        If tagArtiste.IsAncestorOf(e.OriginalSource) Then tagArtiste.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Artiste")
        If tagTitre.IsAncestorOf(e.OriginalSource) Then tagTitre.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Titre")
        If tagAlbum.IsAncestorOf(e.OriginalSource) Then tagAlbum.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Album")
        If tagCompositeur.IsAncestorOf(e.OriginalSource) Then tagCompositeur.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Compositeur")
        If tagGroupement.IsAncestorOf(e.OriginalSource) Then
            Dim MenuContextuelLocal As New ContextMenu
            RaiseEvent RequeteMenuContextuel("Groupement", MenuContextuelLocal, New RoutedEventHandler(AddressOf MenuDynamique_Click))
            tagGroupement.ContextMenu = MenuContextuelLocal
        End If
        If tagImage.IsAncestorOf(e.OriginalSource) Then tagImage.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Image")
        If tagImageDos.IsAncestorOf(e.OriginalSource) Then tagImageDos.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Image")
        If tagImageLabel.IsAncestorOf(e.OriginalSource) Then tagImageLabel.ContextMenu = Await InfosFichier.CreationMenuContextuelDynamique("Image")
        e.Handled = True
    End Sub
    Private Sub MenuDynamique_Click(ByVal sender As Object, ByVal e As RoutedEventArgs)
        If CType(e.OriginalSource, MenuItem).Name Like "Groupement*" Then
            InfosFichier.Groupement = CType(e.OriginalSource, MenuItem).Header
        ElseIf CType(e.OriginalSource, MenuItem).Name Like "Style*" Then
            InfosFichier.Style = CType(e.OriginalSource, MenuItem).Header
        End If
    End Sub
    Private Sub BPAddImage_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPAddDefault.Click, BPAddDos.Click, BPAddLabel.Click
        If SwitchImageDefautSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.CreationMenuImagesDiscogs("AddImageDefaut", e.OriginalSource)
        If SwitchImageLabelSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.CreationMenuImagesDiscogs("AddImageLabel", e.OriginalSource)
        If SwitchImageDosSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.CreationMenuImagesDiscogs("AddImageDosPochette", e.OriginalSource)
        e.Handled = True
    End Sub
    Private Sub BPSupImage_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPSupDefault.Click, BPSupDos.Click, BPSupLabel.Click
        If SwitchImageDefautSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.SuppressionImage("", SwitchImageDefautSelect)
        If SwitchImageLabelSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.SuppressionImage("Label", SwitchImageLabelSelect)
        If SwitchImageDosSelect.IsAncestorOf(e.OriginalSource) Then InfosFichier.SuppressionImage("Dos pochette", SwitchImageDosSelect)
    End Sub

    Private Sub Recherche_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        RaiseEvent RequeteRecherche(InfosFichier.Artiste)
    End Sub
    Private Sub Informations_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        If tagTitre.Text <> "*" And tagTitre.Text <> "" Then
            Dim Texte As String = RemplaceOccurences(tagArtiste.Text & " " & ExtraitChaine(tagTitre.Text, "", " [", , True), " ", "+")
            Texte = RemplaceOccurences(Texte, "&", "")
            Dim Newurl As String = ""
            Try
                Newurl = "http://www.discogs.com/search?q=" & Texte & "&type=all"
                Dim NewURI As Uri = New Uri(Newurl)
                RaiseEvent RequeteWebBrowser(NewURI)
                e.Handled = True
            Catch ex As Exception
                wpfMsgBox.MsgBoxInfo("Lien URL non valide", "Le lien """ & Newurl & """ est non valide", Nothing)
                ' MsgBox("Le lien """ & Newurl & """ est non valide", MsgBoxStyle.Exclamation Or MsgBoxStyle.OkOnly, "Lien URL non valide")
            End Try
        End If
    End Sub
    Private Sub Extraction_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        InfosFichier.Selected = True
        InfosFichier.ExtractionInfosTitre()
        InfosFichier.Selected = False
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE LA VALIDATION DES DONNEES DU FORMULAIRES-----------
    '***********************************************************************************************
    Private Sub gbEditeurTAG_GotKeyboardFocus(ByVal sender As Object, ByVal e As System.Windows.Input.KeyboardFocusChangedEventArgs) Handles Me.GotKeyboardFocus
        Try
            If wpfApplication.FindAncestor(e.NewFocus, "Grid") IsNot Nothing Then
                If CType(wpfApplication.FindAncestor(e.NewFocus, "Grid"), Grid).Name = "GrilleAffichageImages" Then ImageKeyBoardFocus = True
            End If
            If Not (Me.FindCommonVisualAncestor(e.NewFocus).Equals(Me.FindCommonVisualAncestor(e.OldFocus))) Then _
                    InfosFichier.Selected = True
        Catch ex As Exception
        End Try
    End Sub
    Private Sub gbEditeurTAG_LostKeyboardFocus(ByVal sender As Object, ByVal e As System.Windows.Input.KeyboardFocusChangedEventArgs) Handles Me.LostKeyboardFocus
        Try
            'If ImageKeyBoardFocus Then
            If wpfApplication.FindAncestor(e.NewFocus, "Grid") IsNot Nothing Then
                If Not CType(wpfApplication.FindAncestor(e.NewFocus, "Grid"), Grid).Name = "GrilleAffichageImages" Then AnnulationEditionImage()
            End If
            'End If
            If (TypeOf (e.NewFocus) Is ContextMenu) Or (TypeOf (e.NewFocus) Is ComboBoxItem) Or (TypeOf (e.NewFocus) Is Window) Then Exit Sub
            If Not (Me.FindCommonVisualAncestor(e.OldFocus)).Equals(Me.FindCommonVisualAncestor(e.NewFocus)) Then
                tagStyle.GetBindingExpression(ComboBox.TextProperty).UpdateSource()
                InfosFichier.Selected = False
            End If
        Catch ex As Exception
        End Try
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION DE LA MISE A JOUR DES INFOS FICHIERS------------------
    '***********************************************************************************************
    'Traitement du message lors de la notification de modification d'un fichier
    Private Sub ShellWatcher_Changed(ByVal sender As Object, ByVal e As System.IO.FileSystemEventArgs) Handles ShellWatcher.Changed
        If Not BibliothequeLiee.MiseAJourOk Then
            NotifyShellWatcherFilesChanged(e, Nothing)
        End If
    End Sub
    'Traitement du message lors de la notification de renommage d'un fichier
    Private Sub ShellWatcher_Renamed(ByVal sender As Object, ByVal e As System.IO.RenamedEventArgs) Handles ShellWatcher.Renamed
        If Not BibliothequeLiee.MiseAJourOk Then
            NotifyShellWatcherFilesRenamed(e, Nothing)
        End If
    End Sub

    Public Sub NotifyShellWatcherFilesChanged(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesChanged
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       Dim MiseAJourAFaire As Boolean = False
                                                       For Each NomComplet In InfosFichier.ListeFichiers
                                                           If NomComplet = e.FullPath Then
                                                               MiseAJourAFaire = True
                                                               Exit For
                                                           End If
                                                       Next
                                                       If MiseAJourAFaire Then
                                                           If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                           If InfosFichier.ListeFichiers.Count > 1 Then
                                                               For Each i As String In InfosFichier.ListeFichiers
                                                                   If (i <> e.FullPath) And (File.Exists(e.FullPath)) Then
                                                                       Infos.Add(i)
                                                                   End If
                                                               Next
                                                               InfosFichier.Update(Infos)
                                                           Else
                                                               If File.Exists(e.FullPath) Then InfosFichier.Update(Infos)
                                                           End If

                                                       End If
                                                   End Sub))
    End Sub
    Public Sub NotifyShellWatcherFilesCreated(ByVal e As System.IO.FileSystemEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesCreated
    End Sub
    Public Sub NotifyShellWatcherFilesDeleted(ByVal e As System.IO.FileSystemEventArgs) Implements iNotifyShellUpdate.NotifyShellWatcherFilesDeleted
    End Sub
    Public Sub NotifyShellWatcherFilesRenamed(ByVal e As System.IO.RenamedEventArgs, ByVal Infos As tagID3FilesInfos) Implements iNotifyShellUpdate.NotifyShellWatcherFilesRenamed
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       Dim MiseAJourAFaire As Boolean = False
                                                       If Path.GetExtension(e.FullPath) = ".mp3" Then
                                                           If Path.GetExtension(e.OldFullPath) = ".~p3" And (Path.GetFileNameWithoutExtension(e.OldFullPath) = Path.GetFileNameWithoutExtension(e.FullPath)) Then
                                                               NotifyShellWatcherFilesChanged(e, Infos)
                                                           Else
                                                               For Each NomComplet In InfosFichier.ListeFichiers
                                                                   If NomComplet = e.OldFullPath Then
                                                                       MiseAJourAFaire = True
                                                                       Exit For
                                                                   End If
                                                               Next
                                                               If MiseAJourAFaire Then
                                                                   If Infos Is Nothing Then Infos = New tagID3FilesInfos(e.FullPath)
                                                                   If InfosFichier.ListeFichiers.Count > 1 Then
                                                                       For Each i As String In InfosFichier.ListeFichiers
                                                                           If i <> e.OldFullPath Then
                                                                               Infos.Add(i)
                                                                           End If
                                                                       Next
                                                                       InfosFichier.Update(Infos)
                                                                   Else
                                                                       InfosFichier.Update(Infos)
                                                                   End If
                                                               End If
                                                           End If
                                                       End If
                                                   End Sub))
    End Sub

    'PROCEDURE APPELEE POUR MISE A JOUR IDRELEASE
    Public Sub UpdateIdRelease(ByVal idRelease As String)
        If idRelease <> "" Then
            If InfosFichier.PageWeb <> "" Then
                If InfosFichier.PageWeb = "*" Then
                    If wpfMsgBox.MsgBoxQuestion("Mise a jour idRelease", "Voulez-vous remplacer l'ID de tous les fichiers sélectionnés?", Nothing, _
                                                InfosFichier.ListeFichiers.Count & " fichiers sélectionnés") Then
                        tagPageWeb.Focus()
                        InfosFichier.PageWeb = "http://www.discogs.com/release/" & idRelease
                    End If
                Else
                    If wpfMsgBox.MsgBoxQuestion("Mise a jour idRelease", "Voulez-vous remplacer l'ID du fichier sélectionné?", Nothing, InfosFichier.Artiste & _
                                                " - " & InfosFichier.Titre) Then
                        tagPageWeb.Focus()
                        InfosFichier.PageWeb = "http://www.discogs.com/release/" & idRelease
                    End If
                End If
            Else
                tagPageWeb.Focus()
                InfosFichier.PageWeb = "http://www.discogs.com/release/" & idRelease
            End If
        End If
    End Sub

    Private Sub tagStyle_MouseLeftButtonDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles tagStyle.PreviewMouseLeftButtonDown
        Dim Bordure As Border = wpfApplication.FindAncestor(e.OriginalSource, "Border")
        If Bordure IsNot Nothing Then
            Dim Bouton As Button = wpfApplication.FindAncestor(e.OriginalSource, "Button")
            If Bouton IsNot Nothing Then
                Dim ItemSurvole As ComboBoxItem = Nothing
                Dim DonneeSurvolee As XmlElement
                ItemSurvole = CType(tagStyle.ContainerFromElement(e.OriginalSource), ComboBoxItem)
                DonneeSurvolee = CType(ItemSurvole.Content, XmlElement)
                If Bouton.Name = "BoutonAjouter" Then
                    Dim NomNouveauStyle As String = wpfMsgBox.InputBox("Nouveau style...", tagStyle, "Entrer le nom du nouveau style") ' InputBox("Nouveau nom?")
                    If wpfMsgBox.EtatDialogue Then _
                        tagStyle.Text = tagID3FilesInfosDO.AddListElement(StyleDataProvider.Document, NomNouveauStyle, DonneeSurvolee)
                End If
                If Bouton.Name = "BoutonSupprimer" Then tagID3FilesInfosDO.DelListElement(StyleDataProvider.Document, DonneeSurvolee)
            End If
        End If
    End Sub

    '****************************************************************************************************************************
    '***********************************************GESTION DES INFOS DISCOGS**************************************
    '****************************************************************************************************************************
    Private Sub BPDiscogsImages_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        RaiseEvent RequeteInfosDiscogs(InfosFichier.idRelease, NavigationDiscogsType.release) '"27689", NavigationDiscogsType.release) '
    End Sub
    'Mise a jour des infos discogs suite appel timer
    Delegate Sub TraitementTimer()
    Private Sub TimerDiscogs_Elapsed(ByVal sender As Object, ByVal e As System.Timers.ElapsedEventArgs) Handles TimerDiscogs.Elapsed
        TimerDiscogs.Stop()
        Me.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                     New TraitementTimer(Sub()
                                                             If (InfosFichier.idRelease <> "*") And (InfosFichier.idRelease <> "") Then
                                                                 RaiseEvent RequeteInfosDiscogs(InfosFichier.idRelease, NavigationDiscogsType.updaterelease)
                                                             End If
                                                         End Sub))
    End Sub

End Class

