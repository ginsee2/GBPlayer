Option Compare Text

'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 31/12/11
'DESCRIPTION : Classe d'acces au stream de la librairie BASS
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices
Imports System.Windows.Interop

Public Class BassStream
    '***********************************************************************************************
    '----------------------------------EVENEMENTS DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    '***********************************************************************************************
    '----------------------------------PROPRIETES DE LA CLASSE--------------------------------------
    '***********************************************************************************************
    'Private IDStream As String
    Private hStream As IntPtr
    Private DriveNum As Integer


    Public ReadOnly Property ChannelLenght() As Long
        Get
            If hStream <> 0 Then
                Dim Retour = BASS_ChannelGetLength(hStream, 0)
                If Retour > 0 Then Return Retour
            End If
            Return 0
        End Get
    End Property
    Public ReadOnly Property ChannelDuration() As Long
        Get
            If hStream <> 0 Then
                Dim Retour = BASS_ChannelGetLength(hStream, 0)
                If Retour > 0 Then Return ChannelBytes2Seconds(hStream, Retour)
            End If
            Return 0
        End Get
    End Property
    Public Property ChannelPosition As Long
        Get
            If hStream <> 0 Then Return BASS_ChannelGetPosition(hStream, Enum_Bass_ChannelPosition.BASS_POS_BYTE) Else Return 0
        End Get
        Set(ByVal value As Long)
            If hStream <> 0 Then BASS_ChannelSetPosition(hStream, value, Enum_Bass_ChannelPosition.BASS_POS_BYTE)
        End Set
    End Property
    Public Property ChannelPositionInSecond As Double
        Get
            If hStream <> 0 Then Return BASS_ChannelBytes2Seconds(hStream, ChannelPosition) Else Return 0
        End Get
        Set(ByVal value As Double)
            If hStream <> 0 Then ChannelPosition = BASS_ChannelSeconds2Bytes(hStream, value)
        End Set
    End Property
    Public Property ChannelVolume As Single
        Get
            Dim Valeur As Single = 0
            If hStream <> 0 Then
                If BASS_ChannelGetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_VOL, Valeur) <> 0 Then
                    Return Valeur
                End If
            End If
            Return 0
        End Get
        Set(ByVal value As Single)
            If hStream <> 0 Then BASS_ChannelSetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_VOL, value)
        End Set
    End Property
    Public Property ChannelPan As Single
        Get
            Dim Valeur As Single = 0
            If hStream <> 0 Then
                If BASS_ChannelGetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_PAN, Valeur) <> 0 Then
                    Return Valeur
                End If
            End If
            Return 0
        End Get
        Set(ByVal value As Single)
            If hStream <> 0 Then BASS_ChannelSetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_PAN, value)
        End Set
    End Property
    Public Property ChannelFrequence As Single
        Get
            Dim Valeur As Single = 0
            If hStream <> 0 Then
                If BASS_ChannelGetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_FREQ, Valeur) <> 0 Then
                    Return Valeur
                End If
            End If
            Return 0
        End Get
        Set(ByVal value As Single)
            If hStream <> 0 Then BASS_ChannelSetAttribute(hStream, Enum_Bass_Attribut.BASS_ATTRIB_FREQ, value)
        End Set
    End Property
    Public Property IDStream As String
    Public Property HandleStream As IntPtr
    '***********************************************************************************************
    '----------------------------------CONSTRUCTEUR DE LA CLASSE------------------------------------
    '***********************************************************************************************
    Public Sub New(numDevice As Integer, ByVal FileName As String, ByVal Flags As Enum_Bass_StreamCreate)
        Select Case GetFileExt(FileName)
            Case "MP3", "WAV"
                hStream = BASS_StreamCreateFile(0, FileName, 0, 0, Flags)
                If hStream = 0 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_StreamCreateFile"))
                BASS_ChannelSetDevice(hStream, numDevice)
                HandleStream = hStream
                IDStream = FileName
            Case ".CD"
                Dim DriveNum As Integer = CInt(ExtraitChaine(FileName, "[", "]"))
                Dim CDTrack As Integer = CInt(ExtraitChaine(FileName, "-", "."))
                BASS_CD_SetSpeed(DriveNum, -1)
                hStream = BASS_CD_StreamCreate(DriveNum, CDTrack - 1, Flags)
                If hStream = 0 Then Throw New Exception(BASS_CD.BASS_CD_ErrorGetMessage("BASS_CD_StreamCreate"))
                BASS_ChannelSetDevice(hStream, numDevice)
                IDStream = FileName ' CStr(DriveNum) & "-" & CStr(CDTrack - 1)
                BASS_CD_Door(DriveNum, Enum_BassCD_Door.BASS_CD_DOOR_LOCK)
                ' BassCDDrive.DriveUser(DriveNum) = IIf(Convert, BassCDDrive.EnumDriveUser.Converter, BassCDDrive.EnumDriveUser.Player)
        End Select
    End Sub
    Public Sub New(ByVal NumDrive As Integer, ByVal CDTrack As Integer, ByVal Flags As Enum_Bass_StreamCreate, Optional ByVal Convert As Boolean = False)
        DriveNum = NumDrive
        BASS_CD_SetSpeed(NumDrive, -1)
        hStream = BASS_CD_StreamCreate(DriveNum, CDTrack - 1, Flags)
        If hStream = 0 Then Throw New Exception(BASS_CD.BASS_CD_ErrorGetMessage("BASS_CD_StreamCreate"))
        IDStream = CStr(DriveNum) & "-" & CStr(CDTrack - 1)
        BASS_CD_Door(DriveNum, Enum_BassCD_Door.BASS_CD_DOOR_LOCK)
        BassCDDrive.DriveUser(DriveNum) = IIf(Convert, BassCDDrive.EnumDriveUser.Converter, BassCDDrive.EnumDriveUser.Player)
    End Sub
    Public Sub Play()
        If hStream <> 0 Then
            'ShowVSTPlugin()
            If BASS_ChannelPlay(hStream, False) = 0 Then Throw New Exception(BASS.BASS_ErrorGetMessage("BASS_ChannelPlay"))

            'If BASS_VST_ChannelRemoveDSP(hStream, hvstHandle) <> 0 Then MsgBox("ok") Else MsgBox("non ok")
        End If
    End Sub
    Public Sub StopPlay()
        If hStream <> 0 Then
            BASS_ChannelStop(hStream)
        End If
    End Sub
    Public Sub Pause()
        If hStream <> 0 Then
            BASS_ChannelPause(hStream)
        End If
    End Sub
    Public Sub GetLevel(ByRef Gauche As Integer, ByRef droite As Integer)
        If hStream <> 0 Then
            Dim level As UInt32
            level = BASS_ChannelGetLevel(hStream)
            If level > 0 Then
                Dim newGauche As Integer = LoWord(level)
                Dim newDroite As Integer = HiWord(level)
                If newGauche < &HFFFF Then Gauche = newGauche
                If newDroite < &HFFFF Then droite = newDroite
            End If
        End If
    End Sub
    Public Sub ShowVSTPlugin()
        If hStream <> 0 Then
            Dim Fenetre As Window = New Window

            Dim hvstHandle As IntPtr = BASS_VST_ChannelSetDSP(hStream, "C:\Program Files\Steinberg\VstPlugins\iZotope RX 2 Decrackler.dll", 0, 0)
            If hvstHandle = 0 Then MsgBox(BASS_VST_ErrorGetMessage("BASS_VST_ChannelSetDSP"))
            Dim hInfo As BASS_VST_INFO = New BASS_VST_INFO
            If BASS_VST_GetInfo(hvstHandle, hInfo) <> 0 Then
                If hInfo.hasEditor Then
                    Dim Win As WindowInteropHelper = New WindowInteropHelper(Fenetre)
                    BASS_VST_EmbedEditor(hvstHandle, Win.Handle)
                    Fenetre.Height = hInfo.editorHeight
                    Fenetre.Width = hInfo.editorWidth
                    Fenetre.Show()
                    BASS_VST_EmbedEditor(hvstHandle, Win.Handle)
                End If
            End If
        End If
    End Sub

    '***********************************************************************************************
    '----------------------------------DESTRUCTION DE LA CLASSE-------------------------------------
    '***********************************************************************************************
    Public Sub Close()
        If hStream <> 0 Then
            BASS_StreamFree(hStream)
            hStream = 0
            BASS_CD_Door(DriveNum, Enum_BassCD_Door.BASS_CD_DOOR_UNLOCK)
            BassCDDrive.DriveUser(DriveNum) = BassCDDrive.EnumDriveUser.Libre
        End If
    End Sub
    '***********************************************************************************************
    '----------------------------------PROCEDURE PUBLIQUES-------------------------------------
    '***********************************************************************************************
    Public Function ReadData(ByVal TailleBuffer As Integer) As Byte()
        If hStream <> 0 Then
            Try
                Dim BufferWavePtr = Marshal.AllocHGlobal(CInt(TailleBuffer))
                Dim TailleBufferRetour = BASS_ChannelGetData(hStream, BufferWavePtr, TailleBuffer)
                If TailleBufferRetour > 0 Then
                    '  MsgBox(ChannelPosition)
                    Dim BufferRetour(TailleBufferRetour - 1) As Byte
                    Marshal.Copy(BufferWavePtr, BufferRetour, 0, TailleBufferRetour)
                    Marshal.FreeHGlobal(BufferWavePtr)
                    Return BufferRetour
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Function
    Public Function ReadDataInt(ByVal TailleBuffer As Integer) As Int16()
        If hStream <> 0 Then
            Try
                Dim BufferWavePtr = Marshal.AllocHGlobal(CInt(TailleBuffer * 2))
                Dim TailleBufferRetour = BASS_ChannelGetData(hStream, BufferWavePtr, TailleBuffer * 2)
                If TailleBufferRetour > 0 Then
                    '  MsgBox(ChannelPosition)
                    Dim BufferRetour(TailleBufferRetour / 2 - 1) As Int16
                    Marshal.Copy(BufferWavePtr, BufferRetour, 0, TailleBufferRetour / 2)
                    Marshal.FreeHGlobal(BufferWavePtr)
                    Return BufferRetour
                Else
                    Return Nothing
                End If
            Catch ex As Exception
                MsgBox(ex.Message)
            End Try
        End If
    End Function
    '***********************************************************************************************
    '----------------------------------FONCTIONS UTILITAIRES-------------------------------------
    '***********************************************************************************************
    '---FONCTIONS UTILITAIRES PERMETTANT DE CONVERTIR UNE POSITION EN BYTE VERS DES SECONDES
    Private Function ChannelBytes2Seconds(ByVal Handle As IntPtr, ByVal pos As Long) As Double
        If Handle <> 0 Then Return BASS_ChannelBytes2Seconds(Handle, pos) Else Return 0
    End Function

End Class
