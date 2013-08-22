Option Compare Text
'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 11.0
'DATE : 07/03/13
'DESCRIPTION :Classe voie pour DSMixer
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
'Imports SharpDX ' Microsoft.DirectX
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.IO

Public Class dxMixerLine
    '*************************************************************************
    '----------------------ENUMERATION UTILISEE EN INTERNE--------------------
    '*************************************************************************
    Private Enum StatusLine
        Attente
        Play
        Pause
        Ouvert
        Fin
    End Enum
    '*************************************************************************
    '----------------------CONSTANTES UTILISEES EN INTERNE--------------------
    '*************************************************************************
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Public Event BeforePlay(ByVal IDLine As String, ByVal NomFichier As String)
    Public Event AfterPlay(ByVal IDLine As String, ByVal NomFichier As String)
    Public Event FinTitreAtteinte(ByVal IDLine As String, ByVal NomFichier As String)
    Public Event MiseAJourAffichage(ByVal IDLine As String, ByVal PositionActuelle As Integer, ByVal DureeTotale As Integer,
                                    ByVal Gauche As Integer, ByVal Droite As Integer, ByVal bpm As Single)
    '***********************************************************************************************
    '----------------------PROPRIETES DE LA CLASSE--------------------------------------------------
    '***********************************************************************************************
    Private ActiveDevice As Integer
    Private Status As StatusLine
    Private ID As String
    Private lPlayPosition As Integer
    Private BpmCalcule As Single
    Private IDStreamBpmEnCours As String
    Private Delegate Sub NoArgDelegate()
    Private PositionPauseInSecond As Long
    Private WithEvents RafraicheAffichage As Timers.Timer = New Timers.Timer
    Private DemandeFichierSuivant As Boolean

    '*************************************************************************
    '----------------------CONSTRUCTEUR DE LA CLASSE--------------------------
    '*************************************************************************
    Public Sub New(ByVal SystemeDevice As Integer, LineID As String) ' DirectSound.DirectSound) ', ByVal MixerUser As dxMixer)
        ActiveDevice = SystemeDevice
        Status = StatusLine.Attente
        ID = LineID
        RafraicheAffichage.Interval = 20
    End Sub
    '*************************************************************************
    '----------------------DESTRUCTEUR DE LA CLASSE---------------------------
    '*************************************************************************
    Protected Overrides Sub Finalize()
        CloseLine()
        MyBase.Finalize()
    End Sub
    '*************************************************************************
    '----------------------LECTURE DES PROPRIETES DE LA CLASSE----------------
    '*************************************************************************
    Public ReadOnly Property IsOn() As Boolean
        Get
            If FileStream IsNot Nothing Then Return True
            Return False
        End Get
    End Property
    Public Property FileStream As BassStream
    Public Property Volume As Integer
        Get
            If IsOn Then
                Return CLng(FileStream.ChannelVolume * 100)
            End If
            Return 0
        End Get
        Set(ByVal newVolume As Integer)
            If IsOn Then
                FileStream.ChannelVolume = newVolume / 100
            End If
        End Set
    End Property
    Public Property Frequence As Integer
        Get
            If IsOn Then
                Frequence = FileStream.ChannelFrequence
            Else
                Return 0
            End If
        End Get
        Set(ByVal NouvelleFrequence As Integer)
            If IsOn Then
                FileStream.ChannelFrequence = NouvelleFrequence
            End If
        End Set
    End Property
    Public Property Pan As Integer
        Get
            If IsOn Then
                Return CInt(FileStream.ChannelPan * 100)
            End If
            Return 0
        End Get
        Set(ByVal newPan As Integer)
            If IsOn Then FileStream.ChannelPan = newPan / 100
        End Set
    End Property
    Public Property Position As Double
        Get
            If IsOn Then Return FileStream.ChannelPositionInSecond Else Return 0
        End Get
        Set(ByVal NewPosition As Double)
            If IsOn Then
                FileStream.ChannelPositionInSecond = NewPosition
            End If
        End Set
    End Property
    Public ReadOnly Property IDMixerLine As String
        Get
            Return ID
        End Get
    End Property
    Public ReadOnly Property FileName As String
        Get
            If IsOn Then Return FileStream.IDStream Else Return ""
        End Get
    End Property
    '*************************************************************************
    '----------------------METHODES DE LA CLASSE------------------------------
    '*************************************************************************
    Public Function OpenLine(ByVal AFileName As String) As Boolean
        If Status = StatusLine.Pause And FileName = AFileName Then
            If IsOn Then
                lPlayPosition = PositionPauseInSecond
                Status = StatusLine.Ouvert
                OpenLine = True
                Exit Function
            End If
        End If
        CloseLine()
        FileStream = New BassStream(ActiveDevice, AFileName, Enum_Bass_StreamCreate.BASS_STREAM_AUTOFREE)
        If FileStream.ChannelDuration > 0 Then
            'ID = LineID
            IDStreamBpmEnCours = ""
            BpmCalcule = 0
            PositionPauseInSecond = 0
            Status = StatusLine.Ouvert
            RaiseEvent BeforePlay(ID, AFileName)
            Debug.Print(Volume.ToString)
            Return True
        End If
    End Function
    Public Sub CloseLine()
        If IsOn Then
            StopPlay()
            FileStream.Close()
            FileStream = Nothing
        End If
        RaiseEvent AfterPlay(ID, FileName)
        Status = StatusLine.Attente
    End Sub
    Public Function Play() As Boolean
        If IsOn Then
            FileStream.Play()
            Status = StatusLine.Play
            If Not RafraicheAffichage.Enabled Then RafraicheAffichage.Start()
            DemandeFichierSuivant = False
            Gauche = 0
            Droite = 0
            Return True
        End If
    End Function
    Public Sub StopPlay()
        If IsOn And Status <> StatusLine.Fin Then
            FileStream.StopPlay()
            PositionPauseInSecond = 0
            Status = StatusLine.Fin
            RafraicheAffichage.Enabled = False
        End If
    End Sub
    Public Sub PausePlay()
        If IsOn Then
            PositionPauseInSecond = FileStream.ChannelPositionInSecond
            FileStream.Pause()
            Status = StatusLine.Pause
        End If
    End Sub


    Dim Gauche As Integer
    Dim Droite As Integer
    Private Sub RafraicheAffichage_Elapsed(sender As Object, e As Timers.ElapsedEventArgs) Handles RafraicheAffichage.Elapsed
        If IsOn Then
            Dim NewGauche As Integer = Gauche
            Dim NewDroite As Integer = Droite
            FileStream.GetLevel(NewGauche, NewDroite)
            Gauche = Gauche * 0.96
            Droite = Droite * 0.96
            Gauche = Math.Max(Gauche, NewGauche)
            Droite = Math.Max(Droite, NewDroite)
            Dim DureeTotale As Long = FileStream.ChannelDuration ' Int(ObjetEnLecture.PlayDureeTotale / WaveOctetParSec) 'en seconde
            Dim PAvance As Long = FileStream.ChannelPositionInSecond ' Int(lPlayPosition / WaveOctetParSec) 'en seconde


            If PAvance < 0 Then
                RafraicheAffichage.Enabled = False
                RaiseEvent FinTitreAtteinte(ID, FileName)
                StopPlay()
            Else
                RaiseEvent MiseAJourAffichage(ID, PAvance, DureeTotale, Gauche, Droite, BpmCalcule)
            End If
            If BpmCalcule < 0 Then BpmCalcule = 0
        End If
    End Sub
    Public Sub StartBpmCalculate()
        IDStreamBpmEnCours = FileName
        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input,
                                 New NoArgDelegate(Sub()
                                                       Try
                                                           Dim Bass_FX_Bpm As BassFXBpm = New BassFXBpm()
                                                           AddHandler Bass_FX_Bpm.BassFxNewBpm, AddressOf Bass_Fx_NewBpm
                                                           Dim BPM As Single = Bass_FX_Bpm.GetBPMValue(FileName)
                                                           RemoveHandler Bass_FX_Bpm.BassFxNewBpm, AddressOf Bass_Fx_NewBpm
                                                           If IDStreamBpmEnCours = FileName Then BpmCalcule = -BPM
                                                       Catch ex As Exception
                                                           wpfMsgBox.MsgBoxInfo("ERREUR CALCUL BPM", ex.Message)
                                                       End Try
                                                   End Sub))
    End Sub
    '*************************************************************************
    '----------------------METHODES PRIVEES DE LA CLASSE----------------------
    '*************************************************************************
    '---------------------Reponses aux messages de l'objet de calcul Bpm----------------
    Private Sub Bass_Fx_NewBpm(ByVal IDStream As String, ByVal Bpm As Single)
        If IDStreamBpmEnCours = IDStream Then BpmCalcule = Bpm
    End Sub
End Class