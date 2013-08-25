'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 20/07/10 rev 04/08/10
'DESCRIPTION :Classe de lecteur audio
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Option Compare Text

Imports System.Windows.Controls.Primitives
Imports System.IO
Imports System.Windows.Media.Animation
Imports System.Threading
Imports System
Imports System.Collections.ObjectModel

'***********************************************************************************************
'---------------------------------TYPES PUBLIC DE LA CLASSE------------------------------------
'***********************************************************************************************
Public Class WindowPlayer
    '***********************************************************************************************
    '---------------------------------CONSTANTES PRIVEES DE LA CLASSE-------------------------------
    '***********************************************************************************************
    Private Const GBAU_NOMDOSSIER_TEMP = "GBDev\GBPlayer\Temp\"
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES EVENEMENTS DU CONTROLE
    Event FichierSuivant(ByVal NumPiste As Integer)
    Event FichierCharge(ByVal NomFichier As String)
    Public Event RequeteRecherche(ByVal ChaineRecherche As String)
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'DECLARATION DES VARIABLES DE LA FENETRE
    Private WithEvents Recorder As WindowsRecorder
    Private WithEvents TheMixer As dxMixer
    Private RepDest As String
    Public Property ListePistes As List(Of PistePlayer) = New List(Of PistePlayer)
    Private NumPisteEnCours As String
    Private AnimationEnCours As Boolean
    Private BibliothequeLiee As tagID3Bibliotheque
    Public ReadOnly Property Mixer As dxMixer
        Get
            Return TheMixer
        End Get
    End Property
    Private _DirectXSystemList As New ObservableCollection(Of String)
    Public ReadOnly Property DirectXSystemList As ObservableCollection(Of String)
        Get
            Return _DirectXSystemList
        End Get
    End Property
    Public ReadOnly Property IsOk As Boolean
        Get
            'If ListePistes.Count > 0 Then Return True
            If Mixer IsNot Nothing Then If Mixer.IsOn Then Return True
            Return False
        End Get
    End Property
    '***********************************************************************************************
    '---------------------------------INITIALISATION DE LA CLASSE-----------------------------------
    '***********************************************************************************************
    Sub New(ByVal FenetreParent As Window, ByVal Bibliotheque As tagID3Bibliotheque)
        ' Cet appel est requis par le concepteur.
        InitializeComponent()
        Owner = FenetreParent
        BibliothequeLiee = Bibliotheque
        'RafraicheAffichage.Interval = 50
        RepDest = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) & "\" & GBAU_NOMDOSSIER_TEMP
        If Not Directory.Exists(RepDest) Then Directory.CreateDirectory(RepDest)
        Lecteur.LoadedBehavior = MediaState.Manual
        Lecteur.UnloadedBehavior = MediaState.Stop
    End Sub

    Private Sub WindowPlayer_Initialized(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Initialized
        Me.Height = 0
        'Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
        'ConfigUtilisateur = ConfigPerso.LoadConfig
        ' PlayerVolume.Value = CDbl(ConfigUtilisateur.PLAYERVOLUME0)
    End Sub
    Private Sub WindowPlayer_Loaded(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        'OuvertureLecteur()
        Visibility = Visibility.Collapsed
        'If System.Environment.OSVersion.Platform = PlatformID.Win32NT Then
        ' If System.Environment.OSVersion.Version.Major > 5 Then PlateformVista = True
        ' End If
        TheMixer = New dxMixer(0)
        If TheMixer IsNot Nothing Then
            For Each i In dxMixer.DeviceSoundSystemList
                _DirectXSystemList.Add(i)
            Next
            DirectXList.Text = _DirectXSystemList.Item(TheMixer.ActiveSystem)
        End If
    End Sub
    Private Sub DirectXList_SelectionChanged(ByVal sender As Object, ByVal e As System.Windows.Controls.SelectionChangedEventArgs) Handles DirectXList.SelectionChanged
        If IsOk Then
            Dim ListePisteSupprimee As New Collection(Of PistePlayer)
            For Each i In ListePistes
                ListePisteSupprimee.Add(i)
            Next
            TheMixer.ChangeSystemDevice(_DirectXSystemList.IndexOf(e.AddedItems.Item(0)))
            For Each i In ListePisteSupprimee
                Dim Played As Boolean = i.IsPlayed
                Dim FileName As String = i.FilePlayedName
                Dim PositionEnCours As Integer = i.PlayPosition
                SupprimerPiste(i.IdPiste)
                Dim NewPiste As PistePlayer = AjouterPiste(i.IdPiste)
                NewPiste.PlayFichier(FileName, Not i.IsPlayed, PositionEnCours)
            Next
        End If
    End Sub

    Private HauteurPlayer As Double
    Private Sub OuvertureLecteur()
        While AnimationEnCours
            Thread.Sleep(100)
        End While
        AnimationEnCours = True
        Visibility = Visibility.Visible
        Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        Dim ValeurDecalage As Double
        If HauteurPlayer = 0 Then
            ValeurDecalage = 198
            HauteurPlayer = BordureLecteur.ActualHeight
        Else
            ValeurDecalage = 158
        End If
        HauteurPlayer = HauteurPlayer + ValeurDecalage
        If BordureLecteur IsNot Nothing Then
            Me.Left = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
            Dim AnimationPosition As DoubleAnimation = New DoubleAnimation()
            AnimationPosition.From = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
            AnimationPosition.To = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y - ValeurDecalage
            AnimationPosition.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationPosition.FillBehavior = FillBehavior.Stop
            Me.BeginAnimation(WindowPlayer.TopProperty, AnimationPosition)
            Dim AnimationHauteur As DoubleAnimation = New DoubleAnimation()
            AnimationHauteur.From = BordureLecteur.ActualHeight - 2
            AnimationHauteur.To = HauteurPlayer
            AnimationHauteur.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationHauteur.FillBehavior = FillBehavior.Stop
            AddHandler AnimationHauteur.Completed, AddressOf Event_OuvertureLecteurTerminee
            Me.BeginAnimation(WindowPlayer.HeightProperty, AnimationHauteur)
        End If
    End Sub
    Private Sub Event_OuvertureLecteurTerminee(ByVal sender As Object, ByVal e As EventArgs)
        Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        Me.Height = HauteurPlayer ' 200
        BordureLecteur.Height = Me.Height
        Me.Top = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y - HauteurPlayer + BordureLecteur.ActualHeight
        AnimationEnCours = False
    End Sub
    Private Sub FermetureLecteur()
        While AnimationEnCours
            Thread.Sleep(100)
        End While
        AnimationEnCours = True
        Visibility = Visibility.Visible
        Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        Dim ValeurDecalage As Double
        If HauteurPlayer > 201 Then
            ValeurDecalage = 158
        Else
            ValeurDecalage = 198
        End If
        HauteurPlayer = HauteurPlayer - ValeurDecalage
        If BordureLecteur IsNot Nothing Then
            Me.Left = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).X
            Dim AnimationPosition As DoubleAnimation = New DoubleAnimation()
            AnimationPosition.From = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y
            AnimationPosition.To = Me.PointToScreen(BordureLecteur.TranslatePoint(New Point(0, 0), Me)).Y + ValeurDecalage
            AnimationPosition.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationPosition.FillBehavior = FillBehavior.Stop
            Me.BeginAnimation(WindowPlayer.TopProperty, AnimationPosition)
            Dim AnimationHauteur As DoubleAnimation = New DoubleAnimation()
            AnimationHauteur.From = ActualHeight - 2
            AnimationHauteur.To = HauteurPlayer
            AnimationHauteur.Duration = New Duration(New TimeSpan(0, 0, 2))
            AnimationHauteur.FillBehavior = FillBehavior.Stop
            AddHandler AnimationHauteur.Completed, AddressOf Event_FermetureLecteurTerminee
            Me.BeginAnimation(WindowPlayer.HeightProperty, AnimationHauteur)
        End If
    End Sub
    Private Sub Event_FermetureLecteurTerminee(ByVal sender As Object, ByVal e As EventArgs)
        Dim BordureLecteur = CType(Owner, MainWindow).BordureLecteur
        Me.Height = HauteurPlayer ' 200
        BordureLecteur.Height = Me.Height
        AnimationEnCours = False
        If ActualHeight < 200 Then
            HauteurPlayer = 0
            Visibility = Visibility.Visible
        End If
    End Sub
    '***********************************************************************************************
    '---------------------------------DESTRUCTION DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Protected Overrides Sub Finalize()
        Try
            Thread.Sleep(200)
            For Each i In FileIO.FileSystem.GetFiles(RepDest)
                FileIO.FileSystem.DeleteFile(i)
            Next i
            MyBase.Finalize()
        Catch ex As Exception

        End Try
    End Sub
    Private Sub WindowPlayer_Closed(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Closed
        'BoucleLecture.Stop()
        ' RafraicheAffichage.Stop()
        Me.Close()
    End Sub

    'PROCEDURE DE FERMETURE DU LECTEUR APPELE PAR LA FENETRE MAITRE
    Sub ClosePlayer()
        If ListePistes.Count > 0 Then
            Application.Config.player_volumes("PLAYER0") = ListePistes.First.Volume
            If Recorder IsNot Nothing Then Recorder.CloseRecorder()
            If TheMixer IsNot Nothing Then TheMixer.RemoveAllLines()
            TheMixer = Nothing
        End If
    End Sub

    '**************************************************************************************************************
    '**************************************GESTION DU DRAG AND DROP************************************************
    '**************************************************************************************************************

    '***********************************************************************************************
    '---------------------------------FONCTIONS PUBLIQUES DE LA CLASSE------------------------------
    '***********************************************************************************************
    Dim NumLineMixer As Integer = 10
    Public Function PlayFichier(ByVal NomFichier As String, Optional ByVal Piste As String = "") As Boolean
        Try
            If AnimationEnCours Then Return False
            ' If NumLineMixer = 12 Then NumLineMixer = 10
            ' NumLineMixer += 1
            ' Dim FichierDiscAudio As Boolean = Path.GetExtension(NomFichier) = ".cd"
            If Not Mixer.IsOn Then
                TheMixer = Nothing
                Return False
            End If
            If Piste = "" Then Piste = CStr(NumLineMixer)
            If Not TheMixer.LineExist(Piste) Then
                If TheMixer.IsOn Then
                    'Dim PisteEnCours As PistePlayer = New PistePlayer(Piste, Me)
                    'ListePistes.Add(PisteEnCours)
                    'PilePiste.Children.Add(PisteEnCours)
                    'BibliothequeLiee.SubscribeUpdateShellEvent(PisteEnCours)
                    'Dim ConfigUtilisateur As ConfigPerso = New ConfigPerso
                    'ConfigUtilisateur = ConfigPerso.LoadConfig
                    'PisteEnCours.Volume = CDbl(ConfigUtilisateur.PLAYERVOLUME0)
                    AjouterPiste(Piste)
                    OuvertureLecteur()
                End If
                'Else
                '   If ListePistes.Count > 0 Then
                ' Dim PisteSupprimee As PistePlayer = ListePistes.Single(Function(i As PistePlayer)
                ' If i.IdPiste = Piste Then Return True Else Return False
                '                                                        End Function)
                ' If PisteSupprimee IsNot Nothing Then
                ' BibliothequeLiee.UnSubscribeUpdateShellEvent(PisteSupprimee)
                ' PisteSupprimee.Close()
                ' PilePiste.Children.Remove(PisteSupprimee)
                ' ListePistes.Remove(PisteSupprimee)
                ' PisteSupprimee = Nothing
                ' FermetureLecteur()
                'End If
                'End If
                'Return False
            End If
            Dim PisteSelectionnee As PistePlayer = ListePistes.Single(Function(i As PistePlayer)
                                                                          If i.IdPiste = Piste Then Return True Else Return False
                                                                      End Function)
            If PisteSelectionnee IsNot Nothing Then PisteSelectionnee.PlayFichier(NomFichier)
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
        Return True
    End Function
    Private Function AjouterPiste(ByVal Piste As String) As PistePlayer
        Dim PisteEnCours As PistePlayer = New PistePlayer(Piste, Me)
        ListePistes.Add(PisteEnCours)
        PilePiste.Children.Add(PisteEnCours)
        BibliothequeLiee.SubscribeUpdateShellEvent(PisteEnCours)
        PisteEnCours.Volume = CDbl(Application.Config.player_volumes("PLAYER0"))
        AddHandler PisteEnCours.RequeteRecherche, AddressOf ReqRechercheEventHandler
        AddHandler PisteEnCours.RequeteFichierSuivant, AddressOf ReqFichierSuivantEventHandler
        Return PisteEnCours
        'OuvertureLecteur()
    End Function
    Private Sub SupprimerPiste(ByVal Piste As String)
        If ListePistes.Count > 0 Then
            Dim PisteSupprimee As PistePlayer = ListePistes.Single(Function(i As PistePlayer)
                                                                       If i.IdPiste = Piste Then Return True Else Return False
                                                                   End Function)
            If PisteSupprimee IsNot Nothing Then
                RemoveHandler PisteSupprimee.RequeteRecherche, AddressOf ReqRechercheEventHandler
                BibliothequeLiee.UnSubscribeUpdateShellEvent(PisteSupprimee)
                PisteSupprimee.CloseTrack()
                PilePiste.Children.Remove(PisteSupprimee)
                ListePistes.Remove(PisteSupprimee)
                PisteSupprimee = Nothing
                'FermetureLecteur()
            End If
        End If
    End Sub

    'FONCTION UTILISEES LORS MINIATURISATION
    Public Function GetPrincipalTrack() As PistePlayer
        If PilePiste.Children.Count > 0 Then
            Dim PisteEnCours As PistePlayer = PilePiste.Children.Item(0)
            PilePiste.Children.RemoveAt(0)
            Return PisteEnCours
        Else
            Return Nothing
        End If
    End Function
    Public Sub RestorePrincipalTrack(ByVal Piste As PistePlayer)
        If Piste IsNot Nothing Then PilePiste.Children.Insert(0, Piste)
    End Sub
    '***********************************************************************************************
    '---------------------------------GESTION INTERFACE---------------------------------------------
    '***********************************************************************************************
    'REPOND AUX DEMANDES VENANT DES PISTES
    Private Sub ReqRechercheEventHandler(sender As Object, e As RequeteRechercheEventArgs)
        If e.ID <> "" Then
            RaiseEvent RequeteRecherche("id:" & e.ID)
        Else
            RaiseEvent RequeteRecherche("a:" & e.Artiste & ",t:" & e.Titre)
        End If
    End Sub
    Private Sub ReqFichierSuivantEventHandler(sender As Object, e As EventArgs)
        RaiseEvent FichierSuivant(CType(sender, PistePlayer).IdPiste)
    End Sub

    'PERMET DE VALIDER LES MODIFICATIONS DES INFOS TAG
    Private Sub clsPlayer_Activated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Activated
        For Each i In ListePistes
            i.PlayerActivated()
        Next
    End Sub
    Private Sub clsPlayer_Deactivated(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Deactivated
        For Each i In ListePistes
            i.PlayerDeactivated()
        Next
    End Sub

    '***********************************************************************************************
    '---------------------------------GESTION RECORDER--------------------------------------
    '***********************************************************************************************
    Private Sub BPOpenRecorder_Click(ByVal sender As Object, ByVal e As System.Windows.RoutedEventArgs) Handles BPOpenRecorder.Click
        If Recorder Is Nothing Then
            Recorder = New WindowsRecorder(Owner, BibliothequeLiee, Mixer)
            Recorder.Show()
        End If
    End Sub
    Private Sub Recorder_Closing(sender As Object, e As ComponentModel.CancelEventArgs) Handles Recorder.Closing
        Recorder = Nothing
    End Sub
End Class

