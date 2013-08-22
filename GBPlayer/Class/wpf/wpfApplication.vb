'***********************************************************************************************
'-------------------------------DESCRIPTION DE LA CLASSE----------------------------------------
'***********************************************************************************************
'VERSION : v10.0
'DATE : 31/07/10
'DESCRIPTION :Classe de surcharge de l'objet application pour rajouter des fonctionnalités
'***********************************************************************************************
'-------------------------------FICHIERS A INCLURE----------------------------------------------
'***********************************************************************************************
Imports System
Imports System.Windows.Threading

Public Class wpfApplication
    Inherits Application
    '***********************************************************************************************
    '---------------------------------PROCEDURE SHARED DE LA CLASSE---------------------------------
    '***********************************************************************************************

    '----------------------FONCTION RETOURNANT L'ANCETRE DE L'OBJET CONFORME AU TYPE TRANSMIS--------
    Private Shared MemFindFirstAncestor As Object
    Public Shared Function FindAncestor(ByVal objet As Object, ByVal TypeAncetre As String) As Object
        If VisualTreeHelper.GetParent(objet) Is Nothing Then Return Nothing
        If VisualTreeHelper.GetParent(objet).GetType.Name = TypeAncetre Then
            Return VisualTreeHelper.GetParent(objet)
        Else
            ' Debug.Print(VisualTreeHelper.GetParent(objet).GetType.Name)
            Return FindAncestor(VisualTreeHelper.GetParent(objet), TypeAncetre)
        End If
    End Function
    Public Shared Function FindFirstAncestor(ByVal objet As Object, ByVal TypeAncetre As String) As Object
        If VisualTreeHelper.GetParent(objet) Is Nothing Then Return MemFindFirstAncestor
        If VisualTreeHelper.GetParent(objet).GetType.Name = TypeAncetre Then
            MemFindFirstAncestor = VisualTreeHelper.GetParent(objet)
        End If
        Return FindFirstAncestor(VisualTreeHelper.GetParent(objet), TypeAncetre)
    End Function
    Public Shared Function FindLogicalAncestor(ByVal objet As Object, ByVal TypeAncetre As String) As Object
        If LogicalTreeHelper.GetParent(objet) Is Nothing Then Return Nothing
        If LogicalTreeHelper.GetParent(objet).GetType.Name = TypeAncetre Then
            Return LogicalTreeHelper.GetParent(objet)
        Else
            '  Debug.Print(LogicalTreeHelper.GetParent(objet).GetType.Name)
            Return FindLogicalAncestor(LogicalTreeHelper.GetParent(objet), TypeAncetre)
        End If
    End Function
    Public Shared Function FindChild(Objet As Visual, NomATrouver As String) As Visual
        Dim childVisual As Visual = Objet
        For i = 0 To VisualTreeHelper.GetChildrenCount(Objet) - 1
            childVisual = VisualTreeHelper.GetChild(Objet, i)
            If CType(childVisual, FrameworkElement).Name = NomATrouver Then
                Return childVisual
            Else
                childVisual = FindChild(childVisual, NomATrouver)
                If childVisual IsNot Nothing Then Return childVisual
            End If
        Next
        Return Nothing
    End Function

#Region "ECRITURE PROCEDURE DoEVENTS POUR WPF"
    ' Refaire le DoEvents avec un DispatcherFrame (merci à [ Audrey PETIT ])
    Public Shared Sub DoEvents()
        Dim nestedFrame As DispatcherFrame = New DispatcherFrame()
        Dim exitOperation As DispatcherOperation = Dispatcher.CurrentDispatcher.BeginInvoke(
                                                    DispatcherPriority.Background, exitFrameCallback, nestedFrame)
        Dispatcher.PushFrame(nestedFrame)
        If (exitOperation.Status <> DispatcherOperationStatus.Completed) Then
            exitOperation.Abort()
        End If
    End Sub

    Private Shared exitFrameCallback As DispatcherOperationCallback = New DispatcherOperationCallback(AddressOf ExitFrame)

    Private Shared Function ExitFrame(ByVal state As Object) As Object
        Dim frame As DispatcherFrame = CType(state, DispatcherFrame)
        frame.Continue = False
        Return Nothing
    End Function
#End Region

End Class