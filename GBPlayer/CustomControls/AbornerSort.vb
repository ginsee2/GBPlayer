Imports System.ComponentModel

'***********************************************************************************************
'-------------CLASSE DE DESCRIPTION DU TRIANGLE D'INDICATION DE TRI DANS L'ENTETE LISTE VIEW----
'***********************************************************************************************
Public Class AdornerSort
    Inherits Adorner
    Private Shared ReadOnly _AscGeometry As Geometry = Geometry.Parse("M 0,0 L 8,0 L 4,4 Z")
    Private Shared ReadOnly _DescGeometry As Geometry = Geometry.Parse("M 0,4 L 8,4 L 4,0 Z")
    Private m_Direction As ListSortDirection
    Public Property Direction() As ListSortDirection
        Get
            Return m_Direction
        End Get
        Private Set(ByVal value As ListSortDirection)
            m_Direction = value
        End Set
    End Property

    Public Sub New(ByVal element As UIElement, ByVal dir As ListSortDirection)
        MyBase.New(element)
        Direction = dir
    End Sub

    Protected Overrides Sub OnRender(ByVal drawingContext As DrawingContext)
        MyBase.OnRender(drawingContext)
        If AdornedElement.RenderSize.Width < 25 Then
            Return
        End If
        drawingContext.PushTransform(New TranslateTransform(AdornedElement.RenderSize.Width - 15, (AdornedElement.RenderSize.Height - 4) \ 2))
        If Direction = ListSortDirection.Ascending Then
            drawingContext.DrawGeometry(CType(Me.FindResource("W8BorderOver"), SolidColorBrush), Nothing, _AscGeometry)
        Else
            drawingContext.DrawGeometry(CType(Me.FindResource("W8BorderOver"), SolidColorBrush), Nothing, _DescGeometry)
        End If
        drawingContext.Pop()
    End Sub
End Class
