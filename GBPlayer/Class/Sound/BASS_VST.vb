Imports System.Runtime.InteropServices
Imports System.Text

'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : 10.0
'DATE : 23/03/13
'DESCRIPTION :Module d'importation des fonction librairie BASS_VST

Module BASS_VST
    '***********************************************************************************************
    '-----------------------------ENUMERATIONS UTILISEES--------------------------------------------
    '***********************************************************************************************
    ' Public Enum Enum_BassVST_Door
    ' End Enum


    '***********************************************************************************************
    '-------------------------------STRUCTURES PUBLIQUES--------------------------------------------
    '***********************************************************************************************
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_VST_PARAM_INFO
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)> _
        Public name As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)> _
        Public unit As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=16)> _
        Public display As String
        Public defaultValue As Single
    End Structure
    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Ansi)> _
    Public Structure BASS_VST_INFO
        Public channelHandle As IntPtr
        Public uniqueID As UInt32
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)> _
        Public effectName As String
        Public effectVersion As UInt32
        Public effectVstVersion As UInt32
        Public hostVstVersion As UInt32
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)> _
        Public productName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)> _
        Public vendorName As String
        Public vendorVersion As UInt32
        Public chansIn As UInt32
        Public chansOut As UInt32
        Public initialDelay As UInt32
        Public hasEditor As Int32
        Public editorWidth As UInt32
        Public editorHeight As UInt32
        Public aeffect As IntPtr
        Public isInstrument As UInt32
        Public dspHandle As IntPtr
    End Structure


    '***********************************************************************************************
    '-------------------------------FONCTIONS IMPORTEES---------------------------------------------
    '***********************************************************************************************
    '---Importation de la librairie BASS_VST
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_ChannelSetDSP(ByVal chHandle As IntPtr, dllfile As String, flags As Int32, priority As Int16) As IntPtr
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_ChannelRemoveDSP(ByVal chHandle As IntPtr, vstHandle As IntPtr) As Int32
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_GetParamCount(ByVal vstHandle As IntPtr) As Int16
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_GetParamInfo(ByVal vstHandle As IntPtr, Index As Int16, ByRef ret As BASS_VST_PARAM_INFO) As Int32
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_GetProgramCount(ByVal vstHandle As IntPtr) As Int16
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_GetInfo(ByVal vstHandle As IntPtr, ByRef ret As BASS_VST_INFO) As Int32
    End Function
    <DllImport("Bass_vst.dll")> _
    Public Function BASS_VST_EmbedEditor(ByVal vstHandle As IntPtr, win As IntPtr) As Int32
    End Function


    '---FONCTION UTILITAIRES PERMETTANT DE TESTER LE CODE DE RETOUR
    Private Enum Enum_Bass_VST_Error
        BASS_VST_ERROR_NOINPUTS = 3000
        BASS_VST_ERROR_NOOUTPUTS = 3001
        BASS_VST_ERROR_NOREALTIME = 3002
    End Enum
    Public Function BASS_VST_ErrorGetMessage(ByVal FonctionOrigine As String) As String
        Dim msg As String
        Select Case BASS_ErrorGetCode()
            Case Enum_Bass_VST_Error.BASS_VST_ERROR_NOINPUTS
                msg = "the given VST plugin has no inputs and is probably a VST instrument and no effect "
            Case Enum_Bass_VST_Error.BASS_VST_ERROR_NOOUTPUTS
                msg = "the given VST plugin has no outputs "
            Case Enum_Bass_VST_Error.BASS_VST_ERROR_NOREALTIME
                msg = "the given VST plugin does not support realtime processing "
            Case Else
                msg = BASS_ErrorGetMessage(FonctionOrigine)
        End Select
        Return msg & " - " & FonctionOrigine
    End Function

End Module
