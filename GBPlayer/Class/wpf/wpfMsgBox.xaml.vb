Imports System.Windows.Controls.Primitives

Public Class wpfMsgBox
    Public Shared Property ChaineRetour As String
    Public Shared Property EtatDialogue As Boolean
    Private Sub Button_Click(ByVal sender As System.Object, ByVal e As System.Windows.RoutedEventArgs)
        Dim Bouton As Button = CType(e.OriginalSource, Button)
        Select Case Bouton.Name
            Case "Oui", "OK"
                ChaineRetour = Me.Valeur.Text
                EtatDialogue = True
            Case "Non", "Annuler"
                ChaineRetour = ""
                EtatDialogue = False
        End Select
        Me.Close()
    End Sub
    'PERMET LE DEPLACEMENT DE LA FENETRE
    Private Sub EnteteMsgbox_MouseDown(ByVal sender As Object, ByVal e As System.Windows.Input.MouseButtonEventArgs) Handles EnteteMsgbox.MouseDown
        If e.ChangedButton = MouseButton.Left Then Me.DragMove()
    End Sub

    Public Shared Function InputBox(ByVal TexteQuestion As String, Optional ByVal Parent As UIElement = Nothing, Optional ByVal Explication As String = "") As String
        Try
            Dim FenetreEnCours As wpfMsgBox = New wpfMsgBox
            If Parent IsNot Nothing Then
                FenetreEnCours.Owner = GetWindow(Parent) ' WpfApplication.FindAncestor(Parent, "Window")
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.Manual
                If Placement(Parent) Then
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X
                Else
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y - 200
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X - 300
                End If
            Else
                FenetreEnCours.Owner = Application.Current.MainWindow
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.CenterOwner
            End If
            FenetreEnCours.Titre.Content = TexteQuestion
            FenetreEnCours.TexteExplication.Content = Explication
            FenetreEnCours.Valeur.Focus()
            FenetreEnCours.ShowDialog()
            Return ChaineRetour
        Catch ex As Exception
            Return Interaction.InputBox(Explication, TexteQuestion)
        End Try
    End Function
    Private Delegate Sub NoArgDelegate()
    Public Shared Function MsgBoxInfoThread(ByVal TexteTitre As String, ByVal Texte As String, Optional ByVal Parent As UIElement = Nothing, Optional ByVal Explication As String = "") As Boolean
        Dim ReponseRecue As Boolean = True
        Dim Reponse As Boolean
        Application.Current.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Send,
                                 New NoArgDelegate(Sub()
                                                       ReponseRecue = False
                                                       Reponse = MsgBoxInfo(TexteTitre, Texte, Parent, Explication)
                                                       ReponseRecue = True
                                                   End Sub))
        While (Not ReponseRecue)
        End While
        Return Reponse
    End Function
    Public Shared Function MsgBoxInfo(ByVal TexteTitre As String, ByVal Texte As String, Optional ByVal Parent As UIElement = Nothing, Optional ByVal Explication As String = "") As Boolean
        Try
            Dim FenetreEnCours As wpfMsgBox = New wpfMsgBox
            If Parent IsNot Nothing Then
                FenetreEnCours.Owner = GetWindow(Parent) ' WpfApplication.FindAncestor(Parent, "Window")
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.Manual
                If Placement(Parent) Then
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X
                Else
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y - 200
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X - 300
                End If
            Else
                FenetreEnCours.Owner = Application.Current.MainWindow
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.CenterOwner
            End If
            FenetreEnCours.Titre.Content = TexteTitre
            FenetreEnCours.Texte.Text = Texte
            FenetreEnCours.Texte.Visibility = Windows.Visibility.Visible
            FenetreEnCours.Annuler.Visibility = Windows.Visibility.Hidden
            FenetreEnCours.Valeur.Visibility = Windows.Visibility.Hidden
            FenetreEnCours.GrilleBoutons.ColumnDefinitions(1).Width = CType(New GridLengthConverter().ConvertFromString("0"), GridLength)
            FenetreEnCours.TexteExplication.Content = Explication
            FenetreEnCours.Valeur.Focus()
            FenetreEnCours.ShowDialog()
            Return EtatDialogue
        Catch ex As Exception
            If MsgBox(Texte, , TexteTitre) = MsgBoxResult.Yes Then Return True
        End Try
    End Function
    Public Shared Function MsgBoxQuestion(ByVal TexteTitre As String, ByVal Texte As String, Optional ByVal Parent As UIElement = Nothing,
                                          Optional ByVal Explication As String = "",
                                          Optional ByVal NomBpGauche As String = "Oui",
                                          Optional ByVal NomBpDroit As String = "Non") As Boolean
        Try
            Dim FenetreEnCours As wpfMsgBox = New wpfMsgBox
            If Parent IsNot Nothing Then
                FenetreEnCours.Owner = GetWindow(Parent) ' WpfApplication.FindAncestor(Parent, "Window")
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.Manual
                If Placement(Parent) Then
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X
                Else
                    FenetreEnCours.Top = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y - 200
                    FenetreEnCours.Left = Parent.PointToScreen(Mouse.GetPosition(Parent)).X - 300
                End If
            Else
                FenetreEnCours.Owner = Application.Current.MainWindow
                FenetreEnCours.WindowStartupLocation = WindowStartupLocation.CenterOwner
            End If
            FenetreEnCours.Titre.Content = TexteTitre
            FenetreEnCours.Texte.Text = Texte
            FenetreEnCours.Texte.Visibility = Windows.Visibility.Visible
            FenetreEnCours.Valeur.Visibility = Windows.Visibility.Hidden
            FenetreEnCours.Oui.Content = NomBpGauche
            FenetreEnCours.Annuler.Content = NomBpDroit
            FenetreEnCours.TexteExplication.Content = Explication
            FenetreEnCours.Valeur.Focus()
            FenetreEnCours.Visibility = Windows.Visibility.Visible
            FenetreEnCours.ShowDialog()
            Return EtatDialogue
        Catch ex As Exception
            If MsgBox(Texte, , TexteTitre) = MsgBoxResult.Yes Then Return True
        End Try
    End Function
    Private Shared Function Placement(ByVal Parent As UIElement) As Boolean
        Dim FenetreParente As Window = GetWindow(Parent)
        Dim PositionHauteParent As Double = Parent.PointToScreen(Mouse.GetPosition(Parent)).Y
        Dim PositionGaucheParent As Double = Parent.PointToScreen(Mouse.GetPosition(Parent)).X
        Dim PositionHauteFenetre As Double = FenetreParente.PointToScreen(New Point(0, 0)).Y
        Dim PositionGaucheFenetre As Double = FenetreParente.PointToScreen(New Point(0, 0)).X
        Dim PositionBasseFenetre As Double = FenetreParente.PointToScreen(New Point(FenetreParente.ActualWidth, FenetreParente.ActualHeight)).Y
        Dim PositionDroiteFenetre As Double = FenetreParente.PointToScreen(New Point(FenetreParente.ActualWidth, FenetreParente.ActualHeight)).X
        If PositionHauteParent > (PositionBasseFenetre - 200) Then Return False
        If PositionGaucheParent > (PositionDroiteFenetre - 300) Then Return False
        Return True
    End Function

End Class
