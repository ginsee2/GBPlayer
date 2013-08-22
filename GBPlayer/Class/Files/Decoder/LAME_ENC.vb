'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 02/01/12
'DESCRIPTION :Module d'importation des fonction LAME_ENC
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System.Runtime.InteropServices

Module LAME_ENC
    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    '***STRUCTURE DE CONFIGURATION BE_CONFIG
    <StructLayout(LayoutKind.Sequential)> _
    Public Structure PBE_CONFIG
        Public dwConfig As UInt32          'constante BE_CONFIG_MP3
        'Sous structure LHV1
        ' STRUCTURE INFORMATION
        Public dwStructVersion As UInt32   'constante CURRENT_STRUCT_VERSION
        Public dwStructSize As UInt32      'constante CURRENT_STRUCT_SIZE
        ' BASIC ENCODER SETTINGS
        Public dwSampleRate As UInt32      'Frequence d'echantillonage 44100
        Public dwReSampleRate As UInt32    '0 valeur par defaut ou Frequence de reechantillonage
        Public nMode As Int32              'Constante BE_MP3_MODE_xxx
        Public dwBitrate As UInt32         'Specifie le Bitrate utilisé ou le min en VBR
        Public dwMaxBitrate As UInt32      'Specifie le Bitrate max en VBR
        Public nPreset As Int32            'Quality preset, utilisation LAME_QUALITY_PRESET enum
        Public dwMpegVersion As UInt32     'Constante MPEG1
        Public dwPsyModel As UInt32        '0 - UTILISATION FUTURE
        Public dwEmphasis As UInt32        '0 - UTILISATION FUTURE
        ' BIT STREAM SETTINGS
        Public bPrivate As Int32          'Info BOOL TRUE : 1 ou FALSE : 0
        Public bCRC As Int32              'Info BOOL TRUE : 1 ou FALSE : 0
        Public bCopyright As Int32        'Info BOOL TRUE : 1 ou FALSE : 0
        Public bOriginal As Int32         'Info BOOL TRUE : 1 ou FALSE : 0
        ' VBR STUFF
        Public bWriteVBRHeader As Int32   'Ecrit l'entete de gestion VRB Info BOOL TRUE : 1 ou FALSE : 0
        Public bEnableVBR As Int32        'Active le bitrate variable Info BOOL TRUE : 1 ou FALSE : 0
        Public nVBRQuality As Int32    'En max 0 et min 9
        Public dwVbrAbr_bps As UInt32      '0
        Public nVbrMethod As Int32        'Constante VBR_METHOD_xxx
        Public bNoRes As Int32            'Desactive le réservoir de bits Info BOOL TRUE : 1 ou FALSE : 0
        ' MISC SETTINGS
        Public bStrictIso As Int32        'Utilisation strict de la norme Info BOOL TRUE : 1 ou FALSE : 0
        Public nQuality As UInt16  ' Quality HIGH BYTE should be NOT LOW byte, otherwhise quality=5
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=234)> _
        Public btReserved As Byte()
    End Structure
    '***STRUCTURE DE RECUPERATION VERSION
    <StructLayout(LayoutKind.Sequential)> _
    Public Structure PBE_VERSION
        Public byDLLMajorVersion As Byte   'Num version majeure DLL
        Public byDLLMinorVersion As Byte   'Num version mineure DLL
        Public byMajorVersion As Byte      'Num version majeure LAME
        Public byMinorVersion As Byte      'Num version mineure LAME
        Public byDay As Byte               'Jour de la compilation
        Public byMonth As Byte             'Mois de la compilation
        Public wYear As Short            'Année de la compilation
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=BE_MAX_HOMEPAGE)> _
        Public zHomepage As String
        Public byAlphaLevel As Byte
        Public byBetaLevel As Byte
        Public byMMXEnabled As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=125)> _
        Public btReserved As Byte()
    End Structure

    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beInitStream(ByRef pbeConfig As PBE_CONFIG, ByRef dwSamples As UInt32, _
                                         ByRef dwBufferSize As UInt32, ByRef phbeStream As IntPtr) As Int32
    End Function
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beEncodeChunk(ByVal hbeStream As IntPtr, ByVal nSamples As UInt32, _
                                          ByVal pSamples As IntPtr, ByVal pOutput As IntPtr, ByRef pdwOutput As UInt32) _
                                          As Int32
    End Function
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beDeinitStream(ByVal hbeStream As IntPtr, _
                                          ByVal pOutput As IntPtr, ByRef pdwOutput As UInt32) As Int32
    End Function
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beCloseStream(ByVal hbeStream As IntPtr) As UInt32
    End Function
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beWriteVBRHeader(ByVal lpszFileName As String) As Int32
    End Function
    <DllImport("lame_enc.dll", CharSet:=CharSet.Ansi, CallingConvention:=CallingConvention.Cdecl)> _
    Public Function beVersion(ByRef Version As PBE_VERSION) As Int32
    End Function

    '***********************************************************************************************
    '-------------------------------CONSTANTES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    '***CONSTANTE utilisées dans la structure PBE_VERSION
    Public Const BE_MAX_HOMEPAGE = 128
    '***CONSTANTES dwConfig de la structure BE_CONFIG
    Public Const BE_CONFIG_MP3 = 0
    Public Const BE_CONFIG_LAME = 256
    '***CONSTANTES nMode de la structure BE_CONFIG
    Public Const BE_MP3_MODE_STEREO = 0
    Public Const BE_MP3_MODE_JSTEREO = 1
    Public Const BE_MP3_MODE_DUALCHANNEL = 2
    Public Const BE_MP3_MODE_MONO = 3
    '***CONSTANTES dwMpegVersion de la structure BE_CONFIG
    Public Const MPEG1 = 1
    Public Const MPEG2 = 0
    '***CONSTANTE dwStructVersion de la structure BE_CONFIG
    Public Const CURRENT_STRUCT_VERSION = 1
    '***CONSTANTE dwStructSize de la structure BE_CONFIG
    Public Const CURRENT_STRUCT_SIZE = 331
    '***CONSTANTES nPreset de la structure BE_CONFIG
    Public Const LQP_NOPRESET = -1
    Public Const LQP_NORMAL_QUALITY = 0
    Public Const LQP_LOW_QUALITY = 1
    Public Const LQP_HIGH_QUALITY = 2
    Public Const LQP_VOICE_QUALITY = 3
    Public Const LQP_R3MIX = 4
    Public Const LQP_VERYHIGHT_QUALITY = 5
    Public Const LQP_STANDARD = 6
    Public Const LQP_FAST_STANDARD = 7
    Public Const LQP_EXTREME = 8
    Public Const LQP_FAST_EXTREME = 9
    Public Const LQP_INSANE = 10
    Public Const LQP_ABR = 11
    Public Const LQP_CBR = 12
    Public Const LQP_PHONE = 1000
    Public Const LQP_SW = 2000
    Public Const LQP_AM = 3000
    Public Const LQP_FM = 4000
    Public Const LQP_VOICE = 5000
    Public Const LQP_RADIO = 6000
    Public Const LQP_TAPE = 7000
    Public Const LQP_HIFI = 8000
    Public Const LQP_CD = 9000
    Public Const LQP_STUDIO = 10000
    '***CONSTANTE nVbrMethod de la structure BE_CONFIG
    Public Const VBR_METHOD_NONE = -1
    Public Const VBR_METHOD_DEFAULT = 0
    Public Const VBR_METHOD_OLD = 1
    Public Const VBR_METHOD_NEW = 2
    Public Const VBR_METHOD_MTRH = 3
    Public Const VBR_METHOD_ABR = 4

    '***********************************************************************************************
    '-------------------------------FONCTION UTILITAIRE DE GESTION DES ERREURS----------------------
    '***********************************************************************************************
    '***CONSTANTES de retour d'erreurs
    Public Const BE_ERR_SUCCESSFUL = &H0
    Public Const BE_ERR_INVALID_FORMAT = &H1
    Public Const BE_ERR_INVALID_FORMAT_PARAMETERS = &H2
    Public Const BE_ERR_NO_MORE_HANDLES = &H3
    Public Const BE_ERR_INVALID_HANDLE = &H4
    Public Const BE_ERR_BUFFER_TOO_SMALL = &H5
    Public Function LAME_ENC_GetErrorMessage(ByVal val As Long) As String
        Dim msg As String
        Select Case val
            Case BE_ERR_SUCCESSFUL
                Return ""
            Case BE_ERR_INVALID_FORMAT
                msg = "BE_ERR_INVALID_FORMAT"
            Case BE_ERR_INVALID_FORMAT_PARAMETERS
                msg = "BE_ERR_INVALID_FORMAT_PARAMETERS"
            Case BE_ERR_NO_MORE_HANDLES
                msg = "BE_ERR_NO_MORE_HANDLES"
            Case BE_ERR_INVALID_HANDLE
                msg = "BE_ERR_INVALID_HANDLE"
            Case BE_ERR_BUFFER_TOO_SMALL
                msg = "BE_ERR_BUFFER_TOO_SMALL"
            Case Else
                msg = "Unknown MM Error!"
        End Select
        Return msg
    End Function

End Module
