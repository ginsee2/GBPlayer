'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 23/12/11
'DESCRIPTION :Classe lecteur audio multi-voies a travers DIRECTX
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Windows.Interop
Imports System.Runtime.InteropServices
Imports System.Collections.ObjectModel

Public Class dxMixer
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    Public Event BeforePlay(ByVal IDLine As String, ByVal FileName As String)
    Public Event AfterPlay(ByVal IDLine As String, ByVal FileName As String)
    Public Event FinTitreAtteinte(ByVal IDLine As String, ByVal FileName As String)
    Public Event MiseAJourAffichage(ByVal IDLine As String, ByVal PositionActuelle As Integer, ByVal DureeTotale As Integer,
                                    ByVal Gauche As Integer, ByVal Droite As Integer, ByVal bpm As Single)
    '***********************************************************************************************
    '----------------------PROPRIETES DE LA CLASSE--------------------------------------------------
    '***********************************************************************************************
    Private ActiveDevice As Integer ' DirectSound.DirectSound

    Private NumActifSystem As Long
    Private LineCollection As New Collection
    Private Bass_Object As BassSystem
    Private Shared DeviceCount As Integer
    Private Shared DeviceCollection As New ObservableCollection(Of String)

    ' Private SecondaryActiveDevice As DirectSound.DirectSound
    '*************************************************************************
    '----------------------CONSTRUCTEUR DE LA CLASSE--------------------------
    '*************************************************************************
    Shared Sub New()
        Dim info As BASS_DEVICEINFO = New BASS_DEVICEINFO
        While (BASS_GetDeviceInfo(DeviceCount, info) <> 0)
            If (info.Flags And Enum_Bass_Deviceinfo_Flags.BASS_DEVICE_ENABLED) > 0 Then
                If DeviceCount = 0 Then
                    DeviceCollection.Add("<" & DeviceCount & ">- Default")
                Else
                    DeviceCollection.Add("<" & DeviceCount & ">- " & Marshal.PtrToStringAnsi(info.Name))
                End If
                DeviceCount += 1
            End If
        End While
    End Sub
    Public Sub New(ByVal NewNumSysteme As Integer)      'Constructeur d'une classe
        PowerOn(NewNumSysteme)
    End Sub

    '*************************************************************************
    '----------------------DESTRUCTEUR DE LA CLASSE---------------------------
    '*************************************************************************
    Protected Overrides Sub Finalize()
        RemoveAllLines()
        MyBase.Finalize()
    End Sub
    '*************************************************************************
    '----------------------LECTURE DES PROPRIETES DE LA CLASSE----------------
    '*************************************************************************
    Public ReadOnly Property IsOn() As Boolean
        Get
            If Bass_Object IsNot Nothing Then Return True Else Return False
        End Get
    End Property
    Public ReadOnly Property ActiveSystem As Integer
        Get
            Return NumActifSystem
        End Get
    End Property
    Public ReadOnly Property LineExist(ByVal LineName As String) As Boolean
        Get
            Dim Objet As dxMixerLine
            Try
                Objet = LineCollection.Item(LineName)
                If Not Objet Is Nothing Then Return True
            Catch ex As Exception
            End Try
            Return False
        End Get
    End Property
    Public ReadOnly Property LineByName(ByVal LineName As String) As dxMixerLine
        Get
            If LineExist(LineName) Then Return LineCollection.Item(LineName)
        End Get
    End Property

    Public Shared ReadOnly Property DeviceSoundCount() As Long
        Get
            Return DeviceCount
        End Get
    End Property
    Public Shared ReadOnly Property DeviceSoundSystemList As ObservableCollection(Of String)
        Get
            Return DeviceCollection
        End Get
    End Property

    '***********************************************************************************************
    '----------------------METHODES DE LA CLASSE----------------------------------------------------
    '***********************************************************************************************
    Public Function ChangeSystemDevice(ByVal NewSystem As Integer) As Boolean
        RemoveAllLines()
        Return PowerOn(NewSystem)
    End Function
    Private Function PowerOn(ByVal PrincipalNumSysteme As Long) As Boolean
        Try
            If PrincipalNumSysteme <= 0 Or PrincipalNumSysteme >= DeviceSoundCount Then If DeviceSoundCount > 0 Then PrincipalNumSysteme = 0
            Dim _windowHandle As IntPtr = New WindowInteropHelper(Application.Current.MainWindow).Handle
            ActiveDevice = PrincipalNumSysteme ' New DirectSound.DirectSound(New Guid(DIRECTXGuid(PrincipalNumSysteme)))
            'ActiveDevice.SetCooperativeLevel(_windowHandle, DirectSound.CooperativeLevel.Priority)
            Bass_Object = New BassSystem(44100, DeviceSoundSystemList)
            NumActifSystem = PrincipalNumSysteme
            Return True
        Catch ex As Exception
        End Try
        Return False
    End Function
    Public Function AddLine(ByVal LineName As String) As Boolean
        If IsOn Then
            If Not LineExist(LineName) Then
                Dim DSLine As dxMixerLine
                DSLine = New dxMixerLine(ActiveDevice, LineName) ', Me)
                LineCollection.Add(DSLine, CStr(LineName))
                AddHandler DSLine.AfterPlay, AddressOf AfterPlayLine
                AddHandler DSLine.BeforePlay, AddressOf BeforePlayLine
                AddHandler DSLine.FinTitreAtteinte, AddressOf FinTitreAtteinteLine
                AddHandler DSLine.MiseAJourAffichage, AddressOf MiseAJourAffichageLine
                Return True
            End If
        End If
        Return False
    End Function
    Public Function RemoveLine(ByVal LineName As String) As Boolean
        Dim DSLine As dxMixerLine
        If IsOn Then
            If LineExist(LineName) Then
                DSLine = LineCollection.Item(LineName)
                If Not DSLine Is Nothing Then
                    DSLine.StopPlay()
                    DSLine.CloseLine()
                    RemoveHandler DSLine.AfterPlay, AddressOf AfterPlayLine
                    RemoveHandler DSLine.BeforePlay, AddressOf BeforePlayLine
                    RemoveHandler DSLine.FinTitreAtteinte, AddressOf FinTitreAtteinteLine
                    RemoveHandler DSLine.MiseAJourAffichage, AddressOf MiseAJourAffichageLine
                    DSLine = Nothing
                    Call LineCollection.Remove(LineName)
                    Return True
                End If
            End If
        End If
        Return False
    End Function
    Public Function RemoveAllLines() As Boolean
        Dim i As Long
        Try
            If IsOn Then
                For i = LineCollection.Count To 1 Step -1
                    Call RemoveLine(LineCollection.Item(i).IDMixerLine)
                Next i
                Return True
            End If
        Catch ex As Exception
        End Try
        Return False
    End Function
    'Public Function OpenLine(ByVal LineName As String, ByVal strFileName As String) As Boolean
    ' Dim DSLine As dxMixerLine
    '     If IsOn Then
    '         If LineExist(LineName) Then
    '             DSLine = LineCollection.Item(LineName)
    '            If DSLine.OpenLine(LineName, strFileName, Me) Then Return True
    '        End If
    '    End If
    '    Return False
    'End Function
    'Public Function CloseLine(ByVal LineName As String) As Boolean
    ' Dim Line As dxMixerLine
    '     If IsOn Then
    '         If LineExist(LineName) Then
    '             Line = LineCollection.Item(LineName)
    '             If Not Line Is Nothing Then
    ' 'If Line.Status = "PLAY" Or Line.Status = "FIN" Then
    '                 Line.StopPlay()
    ' '                     RaiseEvent AfterPlay(LineName)
    ' 'End If
    '                 Line.CloseLine()
    '                 Return True
    '             End If
    '         End If
    '     End If
    '     Return False
    ' End Function

    '***********************************************************************************************
    '----------------------REPONSES AUX EVEMENENTS DES PISTES---------------------------------------
    '***********************************************************************************************
    Private Sub BeforePlayLine(ByVal IDLine As String, ByVal FileName As String)
        RaiseEvent BeforePlay(IDLine, FileName)
    End Sub
    Private Sub AfterPlayLine(ByVal IDLine As String, ByVal FileName As String)
        RaiseEvent AfterPlay(IDLine, FileName)
    End Sub
    Private Sub FinTitreAtteinteLine(ByVal IDLine As String, ByVal FileName As String)
        RaiseEvent FinTitreAtteinte(IDLine, FileName)
    End Sub
    Private Sub MiseAJourAffichageLine(ByVal IDLine As String, ByVal PositionActuelle As Integer, ByVal DureeTotale As Integer,
                                    ByVal Gauche As Integer, ByVal Droite As Integer, ByVal Bpm As Single)
        RaiseEvent MiseAJourAffichage(IDLine, PositionActuelle, DureeTotale, Gauche, Droite, Bpm)
    End Sub

End Class
