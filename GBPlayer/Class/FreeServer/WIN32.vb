Imports System.Runtime.InteropServices

Module WIN32
    Const HW_PROFILE_GUIDLEN = 39
    Const MAX_PROFILE_LEN = 80

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure HW_PROFILE_INFO
        Public dwDocInfo As Int32
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=HW_PROFILE_GUIDLEN)> _
        Public szHwProfileGuid As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=MAX_PROFILE_LEN)> _
        Public szHwProfileName As String
    End Structure

    <DllImport("advapi32.dll", CharSet:=CharSet.Unicode)> _
    Public Function GetCurrentHwProfileA(ByRef lpHwProfileInfo As HW_PROFILE_INFO) As Int32
    End Function

    Function GetProfileGUID() As String
        Dim St As HW_PROFILE_INFO = New HW_PROFILE_INFO
        GetCurrentHwProfileA(St)
        Return St.szHwProfileGuid
    End Function

End Module
