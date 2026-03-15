Imports System.Drawing

Public Class UIConfig
    Public Property IsDarkMode As Boolean = True
    Public Property LastRunTimestamp As DateTime?
    Public Property RunAtStartup As Boolean = False

    Public ReadOnly Property BackColor As Color
        Get
            Return If(IsDarkMode, Color.Black, Color.White)
        End Get
    End Property

    Public ReadOnly Property ForeColor As Color
        Get
            Return If(IsDarkMode, Color.WhiteSmoke, Color.Black)
        End Get
    End Property

    Public ReadOnly Property InputBackColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(30, 30, 30), Color.White)
        End Get
    End Property

    Public ReadOnly Property InputForeColor As Color
        Get
            Return If(IsDarkMode, Color.White, Color.Black)
        End Get
    End Property

    Public ReadOnly Property ButtonBackColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(45, 45, 48), Color.FromArgb(230, 230, 230))
        End Get
    End Property

    Public ReadOnly Property ButtonForeColor As Color
        Get
            Return If(IsDarkMode, Color.White, Color.Black)
        End Get
    End Property

    Public ReadOnly Property GridBackground As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(30, 30, 30), Color.White)
        End Get
    End Property

    Public ReadOnly Property GridDefaultCellBack As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(45, 45, 48), Color.White)
        End Get
    End Property

    Public ReadOnly Property GridAltRowBack As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(37, 37, 38), Color.FromArgb(245, 245, 245))
        End Get
    End Property

    Public ReadOnly Property GridGridColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(60, 60, 60), Color.FromArgb(200, 200, 200))
        End Get
    End Property

    Public ReadOnly Property SelectionBackColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(70, 70, 74), Color.LightBlue)
        End Get
    End Property

    Public ReadOnly Property SelectionForeColor As Color
        Get
            Return If(IsDarkMode, Color.White, Color.Black)
        End Get
    End Property

    Public ReadOnly Property LogBackColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(18, 18, 18), Color.White)
        End Get
    End Property

    Public ReadOnly Property LogForeColor As Color
        Get
            Return If(IsDarkMode, Color.LightGreen, Color.Black)
        End Get
    End Property

    Public ReadOnly Property HeaderBackColor As Color
        Get
            Return If(IsDarkMode, Color.FromArgb(45, 45, 48), Color.FromArgb(240, 240, 240))
        End Get
    End Property

    Public ReadOnly Property HeaderForeColor As Color
        Get
            Return If(IsDarkMode, Color.White, Color.Black)
        End Get
    End Property
End Class
