Imports System.Runtime.InteropServices

'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'importation des fonction librairie BASS

Module BASS
    '*************************************************************************
    '----------------------ENUMERATIONS PUBLIQUES--------------------
    '*************************************************************************
    Public Enum Enum_Bass_Device
        BASS_DEVICE_8BITS = 1     'use 8 bit resolution, else 16 bit
        BASS_DEVICE_MONO = 2      'use mono, else stereo
        BASS_DEVICE_3D = 4        'enable 3D functionality
        BASS_DEVICE_LATENCY = 256 'calculate device latency (BASS_INFO struct)
        BASS_DEVICE_CPSPEAKERS = 1024 'detect speakers via Windows control panel
        BASS_DEVICE_SPEAKERS = 2048 'force enabling of speaker assignment
        BASS_DEVICE_NOSPEAKER = 4096 'ignore speaker arrangement
    End Enum
    Public Enum Enum_Bass_Sample
        BASS_SAMPLE_8BITS = 1          ' 8 bit
        BASS_SAMPLE_FLOAT = 256        ' 32-bit floating-point
        BASS_SAMPLE_MONO = 2           ' mono
        BASS_SAMPLE_LOOP = 4           ' looped
        BASS_SAMPLE_3D = 8             ' 3D functionality
        BASS_SAMPLE_SOFTWARE = 16      ' not using hardware mixing
        BASS_SAMPLE_MUTEMAX = 32       ' mute at max distance (3D only)
        BASS_SAMPLE_VAM = 64           ' DX7 voice allocation & management
        BASS_SAMPLE_FX = 128           ' old implementation of DX8 effects
        BASS_SAMPLE_OVER_VOL = &H10000 ' override lowest volume
        BASS_SAMPLE_OVER_POS = &H20000 ' override longest playing
        BASS_SAMPLE_OVER_DIST = &H30000 ' override furthest from listener (3D only)
    End Enum
    Public Enum Enum_Bass_Stream
        BASS_STREAM_PRESCAN = &H20000   ' enable pin-point seeking/length (MP3/MP2/MP1)
        BASS_MP3_SETPOS = BASS_STREAM_PRESCAN
        BASS_STREAM_AUTOFREE = &H40000 ' automatically free the stream when it stop/ends
        BASS_STREAM_RESTRATE = &H80000 ' restrict the download rate of internet file streams
        BASS_STREAM_BLOCK = &H100000   ' download/play internet file stream in small blocks
        BASS_STREAM_DECODE = &H200000  ' don't play the stream, only decode (BASS_ChannelGetData)
        BASS_STREAM_STATUS = &H800000  ' give server status info (HTTP/ICY tags) in DOWNLOADPROC
    End Enum

    Public Enum Enum_Bass_ChannelPosition
        BASS_POS_BYTE = 0          ' byte position
        BASS_POS_MUSIC_ORDER = 1   ' order.row position, MAKELONG(order,row)
        BASS_POS_DECODE = &H10000000 ' flag: get the decoding (not playing) position
        BASS_POS_DECODETO = &H20000000 ' flag: decode to the position instead of seeking
        BASS_MUSIC_POSRESET = 32768  ' stop all notes when moving position
        BASS_MUSIC_POSRESETEX = &H400000 ' stop all notes and reset bmp/etc when moving position
    End Enum
    Public Enum Enum_Bass_SetConfig
        BASS_CONFIG_BUFFER = 0
        BASS_CONFIG_UPDATEPERIOD = 1
        BASS_CONFIG_GVOL_SAMPLE = 4
        BASS_CONFIG_GVOL_STREAM = 5
        BASS_CONFIG_GVOL_MUSIC = 6
        BASS_CONFIG_CURVE_VOL = 7
        BASS_CONFIG_CURVE_PAN = 8
        BASS_CONFIG_FLOATDSP = 9
        BASS_CONFIG_3DALGORITHM = 10
        BASS_CONFIG_NET_TIMEOUT = 11
        BASS_CONFIG_NET_BUFFER = 12
        BASS_CONFIG_PAUSE_NOPLAY = 13
        BASS_CONFIG_NET_PREBUF = 15
        BASS_CONFIG_NET_PASSIVE = 18
        BASS_CONFIG_REC_BUFFER = 19
        BASS_CONFIG_NET_PLAYLIST = 21
        BASS_CONFIG_MUSIC_VIRTUAL = 22
        BASS_CONFIG_VERIFY = 23
        BASS_CONFIG_UPDATETHREADS = 24
        BASS_CONFIG_DEV_BUFFER = 27
        BASS_CONFIG_DEV_DEFAULT = 36
        BASS_CONFIG_NET_READTIMEOUT = 37
        BASS_CONFIG_CD_FREEOLD = &H10200
        BASS_CONFIG_CD_RETRY = &H10201
        BASS_CONFIG_CD_AUTOSPEED = &H10202
        BASS_CONFIG_CD_SKIPERROR = &H10203
    End Enum
    Public Enum Enum_Bass_Attribut
        BASS_ATTRIB_FREQ = 1
        BASS_ATTRIB_VOL = 2
        BASS_ATTRIB_PAN = 3
        BASS_ATTRIB_EAXMIX = 4
        BASS_ATTRIB_NOBUFFER = 5
        BASS_ATTRIB_CPU = 7
        BASS_ATTRIB_MUSIC_AMPLIFY = &H100
        BASS_ATTRIB_MUSIC_PANSEP = &H101
        BASS_ATTRIB_MUSIC_PSCALER = &H102
        BASS_ATTRIB_MUSIC_BPM = &H103
        BASS_ATTRIB_MUSIC_SPEED = &H104
        BASS_ATTRIB_MUSIC_VOL_GLOBAL = &H105
    End Enum
    Public Enum Enum_Bass_StreamFile
        STREAMFILE_NOBUFFER = 0
        STREAMFILE_BUFFER = 1
        STREAMFILE_BUFFERPUSH = 2
    End Enum
    Public Enum Enum_Bass_Deviceinfo_Flags
        BASS_DEVICE_ENABLED = 1
        BASS_DEVICE_DEFAULT = 2
        BASS_DEVICE_INIT = 4
    End Enum
    Public Enum Enum_Bass_Info_Flags
        DSCAPS_CONTINUOUSRATE = 16L    ' supports all sample rates between min/maxrate
        DSCAPS_EMULDRIVER = 32L        ' device does NOT have hardware DirectSound support
        DSCAPS_CERTIFIED = 64L         ' device driver has been certified by Microsoft
        DSCAPS_SECONDARYMONO = 256L    ' mono
        DSCAPS_SECONDARYSTEREO = 512L  ' stereo
        DSCAPS_SECONDARY8BIT = 1024L   ' 8 bit
        DSCAPS_SECONDARY16BIT = 2048L  ' 16 bit
    End Enum
    Public Enum Enum_Bass_RecordInfo_Formats
        WAVE_FORMAT_1M08 = &H1L          ' 11.025 kHz, Mono,   8-bit
        WAVE_FORMAT_1S08 = &H2L          ' 11.025 kHz, Stereo, 8-bit
        WAVE_FORMAT_1M16 = &H4L          ' 11.025 kHz, Mono,   16-bit
        WAVE_FORMAT_1S16 = &H8L          ' 11.025 kHz, Stereo, 16-bit
        WAVE_FORMAT_2M08 = &H10L         ' 22.05  kHz, Mono,   8-bit
        WAVE_FORMAT_2S08 = &H20L         ' 22.05  kHz, Stereo, 8-bit
        WAVE_FORMAT_2M16 = &H40L         ' 22.05  kHz, Mono,   16-bit
        WAVE_FORMAT_2S16 = &H80L         ' 22.05  kHz, Stereo, 16-bit
        WAVE_FORMAT_4M08 = &H100L        ' 44.1   kHz, Mono,   8-bit
        WAVE_FORMAT_4S08 = &H200L        ' 44.1   kHz, Stereo, 8-bit
        WAVE_FORMAT_4M16 = &H400L        ' 44.1   kHz, Mono,   16-bit
        WAVE_FORMAT_4S16 = &H800L        ' 44.1   kHz, Stereo, 16-bit
    End Enum
    Public Enum Enum_Bass_RecordStart
        BASS_SAMPLE_8BITS = 1          ' 8 bit
        BASS_SAMPLE_FLOAT = 256        ' 32-bit floating-point
        BASS_RECORD_PAUSE = 32768 ' start recording paused
    End Enum
    Public Enum Enum_Bass_RecordGetInput
        BASS_INPUT_TYPE_UNDEF = &H0
        BASS_INPUT_TYPE_DIGITAL = &H1000000
        BASS_INPUT_TYPE_LINE = &H2000000
        BASS_INPUT_TYPE_MIC = &H3000000
        BASS_INPUT_TYPE_SYNTH = &H4000000
        BASS_INPUT_TYPE_CD = &H5000000
        BASS_INPUT_TYPE_PHONE = &H6000000
        BASS_INPUT_TYPE_SPEAKER = &H7000000
        BASS_INPUT_TYPE_WAVE = &H8000000
        BASS_INPUT_TYPE_AUX = &H9000000
        BASS_INPUT_TYPE_ANALOG = &HA000000
    End Enum
    Public Enum Enum_Bass_RecordSetInput
        BASS_INPUT_OFF = &H10000
        BASS_INPUT_ON = &H20000
   End Enum
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_DEVICEINFO
        Public Name As IntPtr
        Public Driver As IntPtr
        Public Flags As Enum_Bass_Deviceinfo_Flags
    End Structure
    Public Structure BASS_INFO
        Public flags As Enum_Bass_Info_Flags
        Public hwsize As Int32
        Public hwfree As Int32
        Public freesam As Int32
        Public free3d As Int32
        Public minrate As Int32
        Public maxrate As Int32
        Public eax As Int32
        Public minbuf As Int32
        Public dsver As Int32
        Public latency As Int32
        Public initflags As Int32
        Public speakers As Int32
        Public freq As Int32
    End Structure
    Public Structure BASS_RECORDINFO
        Public flags As Enum_Bass_Info_Flags
        Public formats As Enum_Bass_RecordInfo_Formats
        Public inputs As Int32
        Public singlein As Int32
        Public freq As Int32
    End Structure
    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    '---Importation de la librairie BASS
    <DllImport("bass.dll")> _
    Public Function BASS_Init(ByVal device As Int32, ByVal freq As UInt32,
                                                       ByVal flags As Enum_Bass_Device,
                                                        ByVal win As IntPtr, ByVal clsid As IntPtr) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ErrorGetCode() As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_Free() As Int32
    End Function
    <DllImport("Bass.dll")> _
    Public Function BASS_GetDeviceInfo(ByVal device As UInt32, ByRef info As BASS_DEVICEINFO) As Int32
    End Function
    <DllImport("Bass.dll")> _
    Public Function BASS_GetInfo(ByRef info As BASS_INFO) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_GetVersion() As UInt32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_SetConfig(ByVal opt As Enum_Bass_SetConfig, ByVal value As UInt32) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_GetConfig(ByVal opt As Enum_Bass_SetConfig) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_GetDevice() As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_SetDevice(Device As Int32) As Int32
    End Function


    '---Importation de la librairie BASS PARTIE STREAM
    <DllImport("bass.dll")> _
    Public Function BASS_StreamCreateFile(ByVal mem As Int32, ByVal file As String,
                                                          ByVal offset As Long, ByVal length As Long,
                                                          ByVal flags As Enum_Bass_StreamCreate) As IntPtr
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_StreamFree(ByVal handle As IntPtr) As Int32
    End Function

    '---Importation de la librairie BASS PARTIE CHANNEL
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelPlay(ByVal handle As IntPtr, ByVal restart As Int32) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelStop(ByVal handle As IntPtr) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelPause(ByVal handle As IntPtr) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelGetLevel(ByVal handle As IntPtr) As UInt32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelGetLength(ByVal handle As IntPtr, ByVal mode As Enum_Bass_ChannelPosition) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelGetAttribute(ByVal handle As IntPtr, ByVal attrib As Enum_Bass_Attribut, ByRef value As Single) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelSetAttribute(ByVal handle As IntPtr, ByVal attrib As Enum_Bass_Attribut, ByVal value As Single) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelSetDevice(ByVal handle As IntPtr, device As Int32) As Int32
    End Function

    <DllImport("bass.dll")> _
    Public Function BASS_ChannelBytes2Seconds(ByVal handle As IntPtr, ByVal pos As Long) As Double
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelSeconds2Bytes(ByVal handle As IntPtr, ByVal pos As Double) As Long
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelGetPosition(ByVal handle As IntPtr, ByVal mode As Enum_Bass_ChannelPosition) As Long
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelSetPosition(ByVal handle As IntPtr, ByVal pos As Long,
                                                              ByVal mode As Enum_Bass_ChannelPosition) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelGetData(ByVal handle As IntPtr, ByVal buffer As IntPtr, ByVal length As UInt32) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_ChannelUpdate(handle As IntPtr, length As Int32) As Int32
    End Function

    '---Importation de la librairie BASS PARTIE RECORD
    <DllImport("bass.dll")> _
    Public Function BASS_RecordInit(ByVal device As Int32) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_RecordFree() As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_RecordGetDevice() As Int32
    End Function
    <DllImport("Bass.dll")> _
    Public Function BASS_RecordSetDevice(ByVal device As UInt32) As Int32
    End Function
    <DllImport("Bass.dll")> _
    Public Function BASS_RecordGetInfo(ByRef info As BASS_RECORDINFO) As Int32
    End Function
    <DllImport("Bass.dll")> _
    Public Function BASS_RecordGetDeviceInfo(ByVal device As UInt32, ByRef info As BASS_DEVICEINFO) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_RecordGetInput(ByVal input As Int32, ByRef volume As Single) As Enum_Bass_RecordGetInput
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_RecordSetInput(ByVal input As Int32, ByVal attrib As Enum_Bass_RecordSetInput, ByVal value As Single) As Int32
    End Function
    <DllImport("bass.dll")> _
    Public Function BASS_RecordGetInputName(ByVal input As Int32) As IntPtr
    End Function

    <DllImport("bass.dll")> _
    Public Function BASS_RecordStart(ByVal freq As UInt32, ByVal chans As UInt32, ByVal flags As Enum_Bass_RecordStart, ByVal proc As RECORDPROC, user As IntPtr) As IntPtr
    End Function
    Public Delegate Function RECORDPROC(ByVal handle As IntPtr, ByVal buffer As IntPtr, ByVal length As Int32, user As IntPtr) As Int32


    '---FONCTIONS UTILITAIRES PERMETTANT DE CREER UN LONG AVEC LOWORD ET HIWORD
    Public Function MakeLong(ByVal LoWord As Integer, ByVal HiWord As Integer) As Integer
        Dim Resultat As Integer = LoWord And &HFFFF&
        HiWord = HiWord And &HFFFF&
        If HiWord And &H8000& Then
            Return Resultat Or (((HiWord And &H7FFF&) * &H10000) Or &H80000000)
        Else
            Return Resultat Or (HiWord * &H10000)
        End If
    End Function
    '---FONCTIONS UTILITAIRES PERMETTANT DE CREER UN LONG AVEC LOWORD ET HIWORD
    Public Function LoWord(ByVal valeur As UInt32) As Integer
        Dim Val As Long = (valeur And &HFFFF)
        Return Val 'eur And &HFFFF&
    End Function
    '---FONCTIONS UTILITAIRES PERMETTANT DE CREER UN LONG AVEC LOWORD ET HIWORD
    Public Function HiWord(ByVal valeur As UInt32) As Integer
        valeur = valeur >> 16
        Return (valeur And &HFFFF)
    End Function
    '---FONCTIONS UTILITAIRES PERMETTANT DE CREER UN LONG AVEC LOWORD ET HIWORD
    Public Function LoByte(ByVal valeur As Int16) As Byte
        Return valeur And &HFF
    End Function
    '---FONCTIONS UTILITAIRES PERMETTANT DE CREER UN LONG AVEC LOWORD ET HIWORD
    Public Function HiByte(ByVal valeur As Int16) As Byte
        valeur = valeur >> 8
        Return (valeur And &HFF)
    End Function


    '---FONCTION UTILITAIRES PERMETTANT DE TESTER LE CODE DE RETOUR
    Private Enum Enum_Bass_Error
        BASS_ERROR_MEM = 1        'memory error
        BASS_ERROR_FILEOPEN = 2   'can't open the file
        BASS_ERROR_DRIVER = 3     'can't find a free sound driver
        BASS_ERROR_BUFLOST = 4    'the sample buffer was lost
        BASS_ERROR_HANDLE = 5     'invalid handle
        BASS_ERROR_FORMAT = 6     'unsupported sample format
        BASS_ERROR_POSITION = 7   'invalid position
        BASS_ERROR_INIT = 8       'BASS_Init has not been successfully called
        BASS_ERROR_START = 9      'BASS_Start has not been successfully called
        BASS_ERROR_ALREADY = 14   'already initialized/paused/whatever
        BASS_ERROR_NOCHAN = 18    'can't get a free channel
        BASS_ERROR_ILLTYPE = 19   'an illegal type was specified
        BASS_ERROR_ILLPARAM = 20  'an illegal parameter was specified
        BASS_ERROR_NO3D = 21      'no 3D support
        BASS_ERROR_NOEAX = 22     'no EAX support
        BASS_ERROR_DEVICE = 23    'illegal device number
        BASS_ERROR_NOPLAY = 24    'not playing
        BASS_ERROR_FREQ = 25      'illegal sample rate
        BASS_ERROR_NOTFILE = 27   'the stream is not a file stream
        BASS_ERROR_NOHW = 29      'no hardware voices available
        BASS_ERROR_EMPTY = 31     'the MOD music has no sequence data
        BASS_ERROR_NONET = 32     'no internet connection could be opened
        BASS_ERROR_CREATE = 33    'couldn't create the file
        BASS_ERROR_NOFX = 34      'effects are not available
        BASS_ERROR_NOTAVAIL = 37  'requested data is not available
        BASS_ERROR_DECODE = 38    'the channel is a "decoding channel"
        BASS_ERROR_DX = 39        'a sufficient DirectX version is not installed
        BASS_ERROR_TIMEOUT = 40   'connection timedout
        BASS_ERROR_FILEFORM = 41  'unsupported file format
        BASS_ERROR_SPEAKER = 42   'unavailable speaker
        BASS_ERROR_VERSION = 43   'invalid BASS version (used by add-ons)
        BASS_ERROR_CODEC = 44     'codec is not available/supported
        BASS_ERROR_ENDED = 45     'the channel/file has ended
        BASS_ERROR_BUSY = 46      'the device is busy
        BASS_ERROR_UNKNOWN = -1   'some other mystery problem    
    End Enum
    Public Function BASS_ErrorGetMessage(ByVal FonctionOrigine As String) As String
        Dim msg As String
        Select Case BASS_ErrorGetCode()
            Case Enum_Bass_Error.BASS_ERROR_MEM
                msg = "memory error"
            Case Enum_Bass_Error.BASS_ERROR_FILEOPEN
                msg = "BE_ERcan't open the fileR_INVALID_FORMAT"
            Case Enum_Bass_Error.BASS_ERROR_DRIVER
                msg = "can't find a free sound driver"
            Case Enum_Bass_Error.BASS_ERROR_BUFLOST
                msg = "the sample buffer was lost"
            Case Enum_Bass_Error.BASS_ERROR_HANDLE
                msg = "invalid handle"
            Case Enum_Bass_Error.BASS_ERROR_FORMAT
                msg = "unsupported sample format"
            Case Enum_Bass_Error.BASS_ERROR_POSITION
                msg = "invalid position"
            Case Enum_Bass_Error.BASS_ERROR_INIT
                msg = "BASS_Init has not been successfully called"
            Case Enum_Bass_Error.BASS_ERROR_START
                msg = "BASS_Start has not been successfully called"
            Case Enum_Bass_Error.BASS_ERROR_ALREADY
                msg = "already initialized/paused/whatever"
            Case Enum_Bass_Error.BASS_ERROR_NOCHAN
                msg = "can't get a free channel"
            Case Enum_Bass_Error.BASS_ERROR_ILLTYPE
                msg = "an illegal type was specified"
            Case Enum_Bass_Error.BASS_ERROR_ILLPARAM
                msg = "an illegal parameter was specified"
            Case Enum_Bass_Error.BASS_ERROR_NO3D
                msg = "no 3D support"
            Case Enum_Bass_Error.BASS_ERROR_NOEAX
                msg = "no EAX support"
            Case Enum_Bass_Error.BASS_ERROR_DEVICE
                msg = "illegal device number"
            Case Enum_Bass_Error.BASS_ERROR_NOPLAY
                msg = "not playing"
            Case Enum_Bass_Error.BASS_ERROR_FREQ
                msg = "illegal sample rate"
            Case Enum_Bass_Error.BASS_ERROR_NOTFILE
                msg = "the stream is not a file stream"
            Case Enum_Bass_Error.BASS_ERROR_NOHW
                msg = "no hardware voices available"
            Case Enum_Bass_Error.BASS_ERROR_EMPTY
                msg = "the MOD music has no sequence data"
            Case Enum_Bass_Error.BASS_ERROR_NONET
                msg = "no internet connection could be opened"
            Case Enum_Bass_Error.BASS_ERROR_CREATE
                msg = "couldn't create the file"
            Case Enum_Bass_Error.BASS_ERROR_NOFX
                msg = "effects are not available"
            Case Enum_Bass_Error.BASS_ERROR_NOTAVAIL
                msg = "requested data is not available"
            Case Enum_Bass_Error.BASS_ERROR_DECODE
                msg = "the channel is a decoding channel"
            Case Enum_Bass_Error.BASS_ERROR_DX
                msg = "a sufficient DirectX version is not installed"
            Case Enum_Bass_Error.BASS_ERROR_TIMEOUT
                msg = "connection timedout"
            Case Enum_Bass_Error.BASS_ERROR_FILEFORM
                msg = "unsupported file format"
            Case Enum_Bass_Error.BASS_ERROR_SPEAKER
                msg = "unavailable speaker"
            Case Enum_Bass_Error.BASS_ERROR_VERSION
                msg = "invalid BASS version (used by add-ons)"
            Case Enum_Bass_Error.BASS_ERROR_CODEC
                msg = "codec is not available/supported"
            Case Enum_Bass_Error.BASS_ERROR_ENDED
                msg = "the channel/file has ended"
            Case Enum_Bass_Error.BASS_ERROR_BUSY
                msg = "the device is busy"
            Case Enum_Bass_Error.BASS_ERROR_UNKNOWN
                msg = "some other mystery problem"
            Case Else
                msg = "Unknown MM Error!"
        End Select
        Return msg & " - " & FonctionOrigine
    End Function
    Public Function BASS_GetTypeRecordInput(ByVal Type As Enum_Bass_RecordGetInput) As String
        Dim msg As String = ""
        Select Case Type And &HFF000000
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_DIGITAL
                msg = "Digital input source"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_LINE
                msg = "Line-in"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_MIC
                msg = "Microphone"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_SYNTH
                msg = "Internal MIDI"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_CD
                msg = "Analog audio CD"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_PHONE
                msg = "Telephone"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_SPEAKER
                msg = "PC speaker"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_WAVE
                msg = "WAVE/PCM output"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_AUX
                msg = "Auxiliary"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_ANALOG
                msg = "Analog"
            Case Enum_Bass_RecordGetInput.BASS_INPUT_TYPE_UNDEF
                msg = "undefined"
            Case Else
                msg = "undefined"
        End Select
        Return msg
    End Function
End Module
