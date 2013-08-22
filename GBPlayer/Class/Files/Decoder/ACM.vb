'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'importation des fonction ACM windows
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices

Module ACM
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    <StructLayout(LayoutKind.Sequential)> _
    Public Structure MMWAVEFORMATEX
        Public wFormatTag As Int16           'Type de format
        Public nChannels As Int16            'Nombre de canaux
        Public nSamplesPerSec As Int32          'Fréquence d'échantillonnage
        Public nAvgBytesPerSec As Int32         'Nombre d'octet par seconde
        Public nBlockAlign As Int16          'Taille du bloc de donnée
        Public wBitsPerSample As Int16       'Nombre de bit par echantillon
        Public cbSize As Int16               'Taille de la section (FACT CHUNCK)
    End Structure
    <StructLayout(LayoutKind.Sequential)> _
    Public Structure MPEGLAYER3WAVEFORMAT
        Public wFormatTag As Int16       'Type de format WAVE_FORMAT_MPEGLAYER3
        Public nChannels As Int16        'Nombre de canaux
        Public nSamplesPerSec As Int32   'Fréquence d'échantillonnage
        Public nAvgBytesPerSec As Int32  '128*(1024/8)
        Public nBlockAlign As Int16      '1
        Public wBitsPerSample As Int16   '0
        Public cbSize As Int16           'MPEGLAYER3_WFX_EXTRA_BYTES
        Public wID As Int16              'Constante de type MPEGLAYER3_ID_xxx
        Public fdwFlags As Int32         'Constante de type MPEGLAYER3_FLAG_xxx
        Public nBlockSize As Int16       'Calcul 144000/Version*bitrate/frequence
        Public nFramesPerBlock As Int16  '1
        Public nCodecDelay As Int16      '1393
    End Structure
    <StructLayout(LayoutKind.Sequential)> _
    Public Structure ACMSTREAMHEADER            ' [ACM STREAM HEADER TYPE]
        Public cbStruct As Int32            ' Taille de la structure en byte
        Public dwStatus As Int32            ' Status du convertisseur
        Public dwUser As Int32              ' Données utilisateur
        Public pbSrc As IntPtr               ' Pointeur sur le buffer contenant la source
        Public cbSrcLength As Int32         ' Taille du buffer source
        Public cbSrcLengthUsed As Int32     ' Taille des données utilisées dans le buffer source
        Public dwSrcUser As Int32           ' Données utilisateur
        Public pbDst As IntPtr               ' Pointeur sur le buffer contenant la destination
        Public cbDstLength As Int32         ' Taille du buffer destination
        Public cbDstLengthUsed As Int32     ' Taille des données utilisées dans le buffer destination
        Public dwDstUser As Int32           ' Données utilisateur
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=10)> _
        Public dwReservedDriver As Int32()  'Reserve
    End Structure
    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi, EntryPoint:="acmStreamOpen")> _
    Public Function acmStreamOpenMp3(ByRef hAS As IntPtr, ByVal hADrv As UInt32,
                                                            ByRef wfxSrc As MPEGLAYER3WAVEFORMAT,
                                                            ByRef wfxDst As MMWAVEFORMATEX,
                                                            ByVal wFltr As UInt32,
                                                            ByVal dwCallback As UInt32,
                                                            ByVal dwInstance As UInt32,
                                                            ByVal fdwOpen As UInt32) As UInt32
    End Function
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi)> _
    Public Function acmStreamClose(ByVal hAS As IntPtr, ByVal dwClose As UInt32) As UInt32
    End Function
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi)> _
    Public Function acmStreamSize(ByVal hAS As IntPtr, ByVal cbInput As UInt32, _
                                                  ByRef dwOutBytes As UInt32, ByVal dwSize As UInt32) As UInt32
    End Function
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi)> _
    Public Function acmStreamPrepareHeader(ByVal hAS As IntPtr, ByRef hASHdr As ACMSTREAMHEADER, _
                                                            ByVal dwPrepare As UInt32) As UInt32
    End Function
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi)> _
    Public Function acmStreamUnprepareHeader(ByVal hAS As IntPtr, ByRef hASHdr As ACMSTREAMHEADER, _
                                                            ByVal dwUnPrepare As UInt32) As UInt32
    End Function
    <DllImport("msacm32.dll", CharSet:=CharSet.Ansi)> _
    Public Function acmStreamConvert(ByVal hAS As IntPtr, ByRef hASHdr As ACMSTREAMHEADER, _
                                                      ByVal dwConvert As UInt32) As UInt32
    End Function
    '***********************************************************************************************
    '-------------------------------CONSTANTES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    '-- CONSTANTES pour les structures MMWAVEFORMATEX et MPEGLAYER3WAVEFORMAT------------
    Public Const WAVE_FORMAT_PCM = &H1      'Format standard WAVE
    Public Const WAVE_FORMAT_MPEGLAYER3 = &H55     'Format standard mp3
    '-- CONSTANTES pour la structure MPEGLAYER3WAVEFORMAT------------
    Public Const MPEGLAYER3_WFX_EXTRA_BYTES = 12
    Public Const MPEGLAYER3_ID_UNKNOWN = 0
    Public Const MPEGLAYER3_ID_MPEG = 1
    Public Const MPEGLAYER3_ID_CONSTANTFRAMESSIZE = 2
    Public Const MPEGLAYER3_FLAG_PADDING_ISO = &H0
    Public Const MPEGLAYER3_FLAG_PADDING_ON = &H1
    Public Const MPEGLAYER3_FLAG_PADDING_OFF = &H2
    '-- CONSTANTES pour les Bits des Formats de acmStreamOpen---------------------------------------
    Public Const ACM_STREAMOPENF_QUERY = &H1&
    Public Const ACM_STREAMOPENF_ASYNC = &H2&
    Public Const ACM_STREAMOPENF_NONREALTIME = &H4&
    '-- CONSTANTES pour les Flags de AcmStreamSize--------------------------------------------------
    Public Const ACM_STREAMSIZEF_SOURCE = &H0&
    Public Const ACM_STREAMSIZEF_DESTINATION = &H1&
    Public Const ACM_STREAMSIZEF_QUERYMASK = &HF&
    '-- CONSTANTES pour les Bits de ACMSTREAMHEADER.fdwStatus---------------------------------------
    Public Const ACMSTREAMHEADER_STATUSF_DONE = &H10000
    Public Const ACMSTREAMHEADER_STATUSF_PREPARED = &H20000
    Public Const ACMSTREAMHEADER_STATUSF_INQUEUE = &H100000
    '-- CONSTANTES pour les Flags de acmStreamConvert-----------------------------------------------
    Public Const ACM_STREAMCONVERTF_BLOCKALIGN = &H4&
    Public Const ACM_STREAMCONVERTF_START = &H10&
    Public Const ACM_STREAMCONVERTF_END = &H20&

    '***********************************************************************************************
    '-------------------------------FONCTION UTILITAIRE DE GESTION DES ERREURS----------------------
    '***********************************************************************************************
    '--CONSTANTES pour les valeurs de retour des fonctions WaveOut...-------------------------------
    Private Const MMSYSERR_NOERROR = 0                 'Pas d'erreur
    Private Const MMSYSERR_BASE = 0                    'Départ numérotation des erreurs MMSYSERR_...
    Private Const MMSYSERR_ERROR = (MMSYSERR_BASE + 1)
    Private Const MMSYSERR_BADDEVICEID = (MMSYSERR_BASE + 2)  'Identifiant du système hors limites
    Private Const MMSYSERR_NOTENABLED = (MMSYSERR_BASE + 3)  ' driver failed enable */
    Private Const MMSYSERR_ALLOCATED = (MMSYSERR_BASE + 4)   ' device already allocated */
    Private Const MMSYSERR_INVALHANDLE = (MMSYSERR_BASE + 5)  'Handle du système non valide
    Private Const MMSYSERR_NODRIVER = (MMSYSERR_BASE + 6)     'Driver système non présent
    Private Const MMSYSERR_NOMEM = (MMSYSERR_BASE + 7) 'Impossible d'allouer ou de verrouiller la mém
    Private Const MMSYSERR_NOTSUPPORTED = (MMSYSERR_BASE + 8) 'Fonction non supportée par le système
    Private Const MMSYSERR_BADERRNUM = (MMSYSERR_BASE + 9)    ' error value out of range */
    Private Const MMSYSERR_INVALFLAG = (MMSYSERR_BASE + 10)   'Flag transmis non valide
    Private Const MMSYSERR_INVALPARAM = (MMSYSERR_BASE + 11)  'Parametre transmis non valide
    Private Const MMSYSERR_LASTERROR = (MMSYSERR_BASE + 20)   ' last error in range */
    '-- CONSTANTES pour les codes de retour des fonctions-------------------------------------------
    Private Const ACMERR_BASE = 512
    Private Const ACMERR_NOTPOSSIBLE = (ACMERR_BASE + 0)
    Private Const ACMERR_BUSY = (ACMERR_BASE + 1)
    Private Const ACMERR_UNPREPARED = (ACMERR_BASE + 2)
    Private Const ACMERR_CANCELED = (ACMERR_BASE + 3)
    Public Function ACM_GetERRORMessage(ByVal val As Long) As String
        Dim msg As String
        Select Case val
            Case MMSYSERR_NOERROR
                Return ""
            Case ACMERR_NOTPOSSIBLE
                msg = "La requete ne peut aboutir"
            Case MMSYSERR_INVALFLAG
                msg = "Au moins un flag est non valide"
            Case MMSYSERR_INVALHANDLE
                msg = "Un handle est non valide"
            Case MMSYSERR_INVALPARAM
                msg = "Au moins un parametre est non valide"
            Case MMSYSERR_NOMEM
                msg = "Impossible l'allouée les ressources nécessaires"
            Case ACMERR_BUSY
                msg = "Le parametre streamHeader est en cours d'utilisation"
            Case ACMERR_UNPREPARED
                msg = "Le parametre streamheader n'est pas valide"
            Case Else
                msg = "Erreur systeme non repertoriée!"
        End Select
        Return msg
    End Function
End Module
