Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Threading.Tasks
Imports System.Runtime.InteropServices
Imports System.Drawing.Imaging

Public Class Main_Form1
    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function DestroyIcon(hIcon As IntPtr) As Boolean
    End Function

    ' Controls created at runtime
    Private txtHost As TextBox
    Private txtDomain As TextBox
    Private txtPassword As TextBox
    Private btnAddDomain As Button
    Private btnManualUpdate As Button
    Private rtbLog As RichTextBox
    Private dgvRecords As DataGridView
    Private btnDeleteRecord As Button
    Private btnUpdateRecord As Button
    Private chkEnableTimer As CheckBox
    Private chkStartup As CheckBox
    Private numInterval As NumericUpDown
    Private updateTimer As System.Windows.Forms.Timer
    Private notify As NotifyIcon
    Private trayMenu As ContextMenuStrip
    Private lastUpdateAllSuccess As Boolean = False
    Private lblCountdown As Label
    Private lblLastRunStatus As Label
    Private secondTimer As System.Windows.Forms.Timer

    ' New IP Status Labels (Moved here to fix the "Private" error)
    Private lblDetectedIp As Label
    Private lblDomainIp As Label

    ' Persistent records and config
    Private records As System.ComponentModel.BindingList(Of NamecheapRecord)
    Private ReadOnly Property ConfigFilePath As String
        Get
            Return Path.Combine(Application.StartupPath, "diau_ddns_config.json")
        End Get
    End Property
    Private ReadOnly Property UiConfigPath As String
        Get
            Return Path.Combine(Application.StartupPath, "diau_ui_config.json")
        End Get
    End Property

    Private uiConfig As UIConfig

    ' --- STARTUP ---
    Private Async Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        InitializeDynamicControls()
        Await LoadConfigAsync()
        RefreshRecordList()
        InitializeTimer()
    End Sub

    ' --- TRAY BEHAVIOR ---
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Me.Hide()
            If notify IsNot Nothing Then
                notify.ShowBalloonTip(2000, "DIAU DDNS", "Updater is still running in the tray.", ToolTipIcon.Info)
            End If
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub RestoreFromTray()
        Me.Show()
        Me.WindowState = FormWindowState.Normal
        Me.BringToFront()
    End Sub

    ' --- UI INITIALIZATION ---
    Private Sub InitializeDynamicControls()
        Dim primaryFont As New Font("Segoe UI", 10.0F)
        Dim labelFore As Color = Color.WhiteSmoke
        Dim inputBack As Color = Color.FromArgb(30, 30, 30)
        Dim inputFore As Color = Color.White
        Dim btnBack As Color = Color.FromArgb(45, 45, 48)
        Dim btnFore As Color = Color.White

        rtbLog = New RichTextBox() With {.Name = "rtbLog", .Dock = DockStyle.Right, .Width = 520, .MinimumSize = New Size(360, 200), .ReadOnly = True, .Font = New Font("Consolas", 10.0F), .BorderStyle = BorderStyle.None, .BackColor = Color.Black, .ForeColor = Color.Lime}
        Dim pnlLeft As New Panel() With {.Name = "pnlLeft", .Dock = DockStyle.Fill, .Padding = New Padding(24), .AutoScroll = True}

        Dim lblTitle As New Label() With {.Name = "lblTitle", .Text = "Namecheap Dynamic IPv4 DNS Updater", .Font = New Font("Segoe UI Semibold", 14.0F), .AutoSize = True, .Location = New Point(20, 20)}

        ' Row 1: The Settings Row
        ' Top-row controls: use a FlowLayoutPanel to automatically arrange and prevent overlap
        chkEnableTimer = New CheckBox() With {.Text = "Enable periodic updates", .Font = primaryFont, .ForeColor = labelFore, .AutoSize = True}
        AddHandler chkEnableTimer.CheckedChanged, AddressOf ChkEnableTimer_CheckedChanged

        numInterval = New NumericUpDown() With {.Minimum = 5, .Maximum = 1440, .Value = 30, .Width = 60, .Font = primaryFont}
        AddHandler numInterval.ValueChanged, AddressOf NumInterval_ValueChanged

        Dim lblMins As New Label() With {.Text = "minutes", .Font = primaryFont, .ForeColor = labelFore, .AutoSize = True}

        Dim chkDark As CheckBox = New CheckBox() With {.Name = "chkDark", .Text = "Dark Mode", .Font = primaryFont, .ForeColor = labelFore, .AutoSize = True}
        AddHandler chkDark.CheckedChanged, AddressOf ChkDark_CheckedChanged

        ' THE MISSING CHECKBOX
        chkStartup = New CheckBox() With {.Name = "chkStartup", .Text = "Run at Startup", .Font = primaryFont, .ForeColor = labelFore, .AutoSize = True}
        AddHandler chkStartup.CheckedChanged, AddressOf ChkStartup_CheckedChanged

        Dim pnlTopRow As New FlowLayoutPanel() With {
            .Name = "pnlTopRow",
            .AutoSize = False,
            .Height = 36,
            .Width = 560,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = True,
            .Location = New Point(20, 60),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
            .Padding = New Padding(0)
        }

        ' Give child controls a small margin so they don't touch
        chkEnableTimer.Margin = New Padding(0, 6, 12, 0)
        numInterval.Margin = New Padding(0, 4, 6, 0)
        lblMins.Margin = New Padding(0, 8, 12, 0)
        chkDark.Margin = New Padding(0, 6, 12, 0)
        chkStartup.Margin = New Padding(0, 6, 12, 0)

        pnlTopRow.Controls.AddRange(New Control() {chkEnableTimer, numInterval, lblMins, chkDark, chkStartup})

        ' Row 2: Status and IP labels
        lblCountdown = New Label() With {.Text = "Periodic updates disabled", .Font = primaryFont, .AutoSize = True, .ForeColor = labelFore}
        lblLastRunStatus = New Label() With {.Text = "Last run: never", .Font = primaryFont, .AutoSize = True, .ForeColor = labelFore}

        lblDetectedIp = New Label() With {.Text = "My Public IP: Detecting...", .Font = primaryFont, .AutoSize = True, .ForeColor = labelFore}
        lblDomainIp = New Label() With {.Text = "DNS points to: Unknown", .Font = primaryFont, .AutoSize = True, .ForeColor = labelFore}

        Dim statusPanel As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.LeftToRight}
        statusPanel.Controls.AddRange(New Control() {lblCountdown, lblLastRunStatus})

        Dim ipPanel As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.LeftToRight}
        ipPanel.Controls.AddRange(New Control() {lblDetectedIp, lblDomainIp})

        ' Row 4-6: Inputs (use TableLayoutPanel for consistent alignment)
        ' Use a fixed width for inputs so they don't expand under the log pane
        Dim inputWidth As Integer = 420
        txtHost = New TextBox() With {.Name = "txtHost", .Font = primaryFont, .BackColor = inputBack, .ForeColor = inputFore, .BorderStyle = BorderStyle.FixedSingle, .Width = inputWidth}
        txtDomain = New TextBox() With {.Name = "txtDomain", .Font = primaryFont, .BackColor = inputBack, .ForeColor = inputFore, .BorderStyle = BorderStyle.FixedSingle, .Width = inputWidth}
        txtPassword = New TextBox() With {.Name = "txtPassword", .Font = primaryFont, .BackColor = inputBack, .ForeColor = inputFore, .BorderStyle = BorderStyle.FixedSingle, .UseSystemPasswordChar = True, .Width = inputWidth}

        Dim inputsPanel As New TableLayoutPanel() With {.AutoSize = True, .Dock = DockStyle.Top, .ColumnCount = 2}
        inputsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        inputsPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Absolute, inputWidth))
        inputsPanel.RowCount = 3
        inputsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputsPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        inputsPanel.Controls.Add(New Label() With {.Text = "Host:", .AutoSize = True}, 0, 0)
        inputsPanel.Controls.Add(txtHost, 1, 0)
        inputsPanel.Controls.Add(New Label() With {.Text = "Domain:", .AutoSize = True}, 0, 1)
        inputsPanel.Controls.Add(txtDomain, 1, 1)
        inputsPanel.Controls.Add(New Label() With {.Text = "Password:", .AutoSize = True}, 0, 2)
        inputsPanel.Controls.Add(txtPassword, 1, 2)

        ' Row 7: Buttons - placed in a FlowLayoutPanel for consistent spacing and layout
        btnAddDomain = New Button() With {.Text = "Save", .Font = primaryFont, .BackColor = btnBack, .ForeColor = btnFore, .FlatStyle = FlatStyle.Flat, .Size = New Size(100, 36)}
        AddHandler btnAddDomain.Click, AddressOf BtnAddDomain_Click

        btnManualUpdate = New Button() With {.Text = "Manual", .Font = primaryFont, .BackColor = Color.FromArgb(70, 70, 74), .ForeColor = btnFore, .FlatStyle = FlatStyle.Flat, .Size = New Size(100, 36)}
        AddHandler btnManualUpdate.Click, AddressOf BtnManualUpdate_Click

        btnUpdateRecord = New Button() With {.Text = "Update", .Font = primaryFont, .BackColor = Color.FromArgb(70, 70, 74), .ForeColor = btnFore, .FlatStyle = FlatStyle.Flat, .Size = New Size(100, 36)}
        AddHandler btnUpdateRecord.Click, AddressOf BtnUpdateRecord_Click

        btnDeleteRecord = New Button() With {.Text = "Delete", .Font = primaryFont, .BackColor = Color.FromArgb(180, 60, 60), .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat, .Size = New Size(100, 36)}
        AddHandler btnDeleteRecord.Click, AddressOf BtnDeleteRecord_Click

        Dim pnlButtons As New FlowLayoutPanel() With {.AutoSize = True, .FlowDirection = FlowDirection.LeftToRight, .Anchor = AnchorStyles.Left Or AnchorStyles.Top, .Dock = DockStyle.Top}
        pnlButtons.Controls.AddRange(New Control() {btnAddDomain, btnManualUpdate, btnUpdateRecord, btnDeleteRecord})

        ' Grid (anchor to left/top/bottom and use fixed width so it doesn't grow under the log)
        dgvRecords = New DataGridView() With {.Name = "dgvRecords", .Font = primaryFont, .AllowUserToAddRows = False, .SelectionMode = DataGridViewSelectionMode.FullRowSelect, .RowHeadersVisible = False, .BorderStyle = BorderStyle.None, .BackgroundColor = Color.FromArgb(30, 30, 30), .GridColor = Color.FromArgb(60, 60, 60), .EnableHeadersVisualStyles = False, .Dock = DockStyle.Fill, .MinimumSize = New Size(400, 200), .ScrollBars = ScrollBars.Both, .AllowUserToResizeColumns = True, .AllowUserToOrderColumns = True}
        dgvRecords.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(45, 45, 48), .ForeColor = Color.White, .Font = New Font(primaryFont.FontFamily, primaryFont.Size, FontStyle.Bold)}
        dgvRecords.DefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(45, 45, 48), .ForeColor = Color.White, .SelectionBackColor = Color.FromArgb(70, 70, 74), .SelectionForeColor = Color.White}
        dgvRecords.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(37, 37, 38), .ForeColor = Color.White}
        ' Disable automatic fill so we can control column widths and keep all columns visible
        dgvRecords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        AddHandler dgvRecords.SelectionChanged, AddressOf DgvRecords_SelectionChanged

        ' Main layout to prevent overlap: rows for title, top row, status, ip, inputs, buttons and the grid filling remaining space
        Dim tblMain As New TableLayoutPanel() With {.Dock = DockStyle.Fill, .ColumnCount = 1, .Padding = New Padding(0)}
        tblMain.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        ' Add an extra spacer row (50px) between buttons and grid to prevent overlap
        tblMain.RowCount = 8
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        tblMain.RowStyles.Add(New RowStyle(SizeType.Absolute, 50))
        tblMain.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        tblMain.Controls.Add(lblTitle, 0, 0)
        tblMain.Controls.Add(pnlTopRow, 0, 1)
        tblMain.Controls.Add(statusPanel, 0, 2)
        tblMain.Controls.Add(ipPanel, 0, 3)
        tblMain.Controls.Add(inputsPanel, 0, 4)
        tblMain.Controls.Add(pnlButtons, 0, 5)
        ' row 6 is spacer
        tblMain.Controls.Add(dgvRecords, 0, 7)

        ' Add tblMain into pnlLeft
        pnlLeft.Controls.Add(tblMain)
        ' Make sure pnlLeft can scroll if window is narrow; keep left area width fixed
        pnlLeft.AutoScrollMinSize = New Size(760, 400)
        inputsPanel.Margin = New Padding(0, 6, 0, 6)
        pnlButtons.Margin = New Padding(0, 6, 0, 6)

        ' Add everything to the screen (add left panel first, then docked log to ensure correct layout)
        Controls.Add(pnlLeft)
        Controls.Add(rtbLog)
        ' Do not change z-order here (BringToFront) because that can cause the log to overlay
        ' the left panel and hide part of the grid. Docking will place controls correctly.

        uiConfig = LoadUiConfig()
        ' Ensure UI controls reflect saved config
        Dim darkCtl = pnlTopRow.Controls.OfType(Of CheckBox)().FirstOrDefault(Function(c) c.Name = "chkDark")
        If darkCtl IsNot Nothing Then darkCtl.Checked = uiConfig.IsDarkMode
        If chkStartup IsNot Nothing Then chkStartup.Checked = uiConfig.RunAtStartup
        ApplyTheme(uiConfig)
    End Sub

    Private Sub RefreshRecordList()
        If dgvRecords Is Nothing Then Return
        If records Is Nothing Then records = New System.ComponentModel.BindingList(Of NamecheapRecord)()
        dgvRecords.DataSource = Nothing
        dgvRecords.AutoGenerateColumns = False
        ' Recreate columns explicitly each time to avoid any stale/hidden columns
        dgvRecords.Columns.Clear()

        ' Create four columns (Host, Domain, Last Status, Last Updated) and use Fill mode
        ' with relative FillWeight so the table appears as a single coherent grid.
        Dim colHost = New DataGridViewTextBoxColumn() With {
            .Name = "Host",
            .DataPropertyName = "Host",
            .HeaderText = "Host",
            .MinimumWidth = 60,
            .Visible = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .FillWeight = 15
        }
        Dim colDomain = New DataGridViewTextBoxColumn() With {
            .Name = "Domain",
            .DataPropertyName = "Domain",
            .HeaderText = "Domain",
            .MinimumWidth = 200,
            .Visible = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .FillWeight = 25
        }
        Dim colStatus = New DataGridViewTextBoxColumn() With {
            .Name = "LastStatus",
            .DataPropertyName = "LastStatus",
            .HeaderText = "Last Status",
            .MinimumWidth = 140,
            .Visible = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .FillWeight = 20
        }
        Dim colUpdated = New DataGridViewTextBoxColumn() With {
            .Name = "LastUpdated",
            .DataPropertyName = "LastUpdated",
            .HeaderText = "Last Updated",
            .MinimumWidth = 120,
            .Visible = True,
            .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            .FillWeight = 20
        }

        dgvRecords.Columns.AddRange(New DataGridViewColumn() {colHost, colDomain, colStatus, colUpdated})

        ' Prefer AllCells for sizing so columns fit their contents and visible area without forcing
        ' a horizontal scroll. Allow Fill as fallback when container is resized smaller.
        dgvRecords.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        dgvRecords.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
        dgvRecords.ColumnHeadersVisible = True
        dgvRecords.DataSource = records

        ' After binding, ensure the grid will show a horizontal scrollbar when necessary
        dgvRecords.ScrollBars = ScrollBars.Both
    End Sub

    ' --- HANDLERS ---
    Private Async Sub BtnManualUpdate_Click(sender As Object, e As EventArgs)
        If records Is Nothing OrElse records.Count = 0 Then
            MessageBox.Show("No DNS records configured.", "Manual Update", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' compute time since last manual/auto run
        Dim timeSinceLastRun = If(uiConfig?.LastRunTimestamp.HasValue,
                                 DateTime.Now - uiConfig.LastRunTimestamp.Value,
                                 TimeSpan.FromMinutes(10))

        Dim currentIp = Await GetPublicIpAsync()
        If String.IsNullOrEmpty(currentIp) Then
            MessageBox.Show("Unable to detect public IP. Try again later.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ' Determine whether any configured domain currently resolves to a different IP
        Dim anyMismatch As Boolean = False
        For Each r In records
            Dim fullDomain = If(r.Host = "@", r.Domain, $"{r.Host}.{r.Domain}")
            Try
                Dim dnsIps = Await System.Net.Dns.GetHostAddressesAsync(fullDomain)
                Dim currentDnsIp = dnsIps.FirstOrDefault()?.ToString()
                If String.IsNullOrEmpty(currentDnsIp) OrElse currentDnsIp <> currentIp Then
                    anyMismatch = True
                    Exit For
                End If
            Catch
                ' treat lookup failure as mismatch so user can attempt to fix it immediately
                anyMismatch = True
                Exit For
            End Try
        Next

        ' If it's been less than 5 minutes since last run and there are no mismatches,
        ' show spam-protection warning and allow user to bypass.
        If timeSinceLastRun.TotalMinutes < 5 AndAlso Not anyMismatch Then
            Dim msg = $"Manual updates are restricted to once every 5 minutes to prevent API abuse.{vbCrLf}{vbCrLf}Your public IP ({currentIp}) already matches DNS for all configured records. Do you want to force an update anyway?"
            Dim result = MessageBox.Show(msg, "Spam Protection", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If result = DialogResult.No Then
                Log("Manual update cancelled by user (Spam Protection).")
                Return
            End If
        End If

        ' If it's been less than 5 minutes but there is a mismatch, allow update (necessary fix)
        If timeSinceLastRun.TotalMinutes < 5 AndAlso anyMismatch Then
            Log("Manual update allowed despite spam-protection because DNS mismatch detected.")
        Else
            Log("Manual update triggered.")
        End If

        Await UpdateAllAsync()
    End Sub

    Private Async Function LoadConfigAsync() As Task
        Try
            If File.Exists(ConfigFilePath) Then
                Dim json = Await File.ReadAllTextAsync(ConfigFilePath)
                Dim cfg = JsonSerializer.Deserialize(Of DdnsConfig)(json)
                If cfg IsNot Nothing Then
                    records = New System.ComponentModel.BindingList(Of NamecheapRecord)(If(cfg.Records, New List(Of NamecheapRecord)()))
                    chkEnableTimer.Checked = cfg.PeriodicEnabled
                    numInterval.Value = If(cfg.PeriodicIntervalMinutes >= 5, cfg.PeriodicIntervalMinutes, 30)
                End If
            End If
        Catch ex As Exception : Log("Load error.") : End Try
        If records Is Nothing Then records = New System.ComponentModel.BindingList(Of NamecheapRecord)()
    End Function

    Private Sub DgvRecords_SelectionChanged(sender As Object, e As EventArgs)
        If dgvRecords.SelectedRows.Count = 0 Then Return
        Dim r = records(dgvRecords.SelectedRows(0).Index)
        txtHost.Text = r.Host : txtDomain.Text = r.Domain : txtPassword.Text = r.Password
    End Sub

    Private Async Sub BtnDeleteRecord_Click(sender As Object, e As EventArgs)
        If dgvRecords.SelectedRows.Count = 0 Then Return
        records.RemoveAt(dgvRecords.SelectedRows(0).Index)
        Await SaveConfigAsync() : RefreshRecordList()
    End Sub

    Private Async Sub BtnUpdateRecord_Click(sender As Object, e As EventArgs)
        If dgvRecords.SelectedRows.Count = 0 Then Return
        Dim r = records(dgvRecords.SelectedRows(0).Index)
        r.Host = txtHost.Text : r.Domain = txtDomain.Text : r.Password = txtPassword.Text
        Await SaveConfigAsync() : RefreshRecordList()
    End Sub

    Private Async Sub BtnAddDomain_Click(sender As Object, e As EventArgs)
        Dim host = If(txtHost IsNot Nothing, txtHost.Text.Trim(), String.Empty)
        Dim domain = If(txtDomain IsNot Nothing, txtDomain.Text.Trim(), String.Empty)
        Dim password = If(txtPassword IsNot Nothing, txtPassword.Text.Trim(), String.Empty)

        If String.IsNullOrEmpty(host) OrElse String.IsNullOrEmpty(domain) OrElse String.IsNullOrEmpty(password) Then
            MessageBox.Show("Please provide Host, Domain and Password.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If records Is Nothing Then records = New System.ComponentModel.BindingList(Of NamecheapRecord)()

        records.Add(New NamecheapRecord() With {.Host = host, .Domain = domain, .Password = password, .LastStatus = "Never", .LastUpdated = DateTime.MinValue})
        Await SaveConfigAsync()
        RefreshRecordList()
        Log($"Added record: {host}.{domain}")

        txtHost.Text = String.Empty
        txtDomain.Text = String.Empty
        txtPassword.Text = String.Empty
    End Sub

    Private Async Sub UpdateTimer_Tick(sender As Object, e As EventArgs)
        Await UpdateAllAsync(True)
    End Sub

    Private Sub InitializeTimer()
        If updateTimer Is Nothing Then updateTimer = New System.Windows.Forms.Timer() : AddHandler updateTimer.Tick, AddressOf UpdateTimer_Tick
        updateTimer.Stop()
        updateTimer.Interval = CInt(numInterval.Value) * 60 * 1000
        If chkEnableTimer.Checked Then updateTimer.Start()

        If secondTimer Is Nothing Then secondTimer = New System.Windows.Forms.Timer() With {.Interval = 1000} : AddHandler secondTimer.Tick, AddressOf SecondTimer_Tick
        secondTimer.Start()

        If notify Is Nothing Then
            trayMenu = New ContextMenuStrip()
            trayMenu.Items.Add("Open Dashboard", Nothing, Sub() RestoreFromTray())
            trayMenu.Items.Add("-")
            trayMenu.Items.Add("Exit DIAU", Nothing, Sub()
                                                         notify.Visible = False
                                                         Application.Exit()
                                                     End Sub)
            notify = New NotifyIcon() With {.Visible = True, .ContextMenuStrip = trayMenu, .Text = "DIAU DDNS Updater"}
            AddHandler notify.DoubleClick, Sub() RestoreFromTray()
        End If
        UpdateTrayStatus()
    End Sub

    Private Sub SecondTimer_Tick(sender As Object, e As EventArgs)
        If Not updateTimer.Enabled Then lblCountdown.Text = "Periodic updates disabled" : Return
        Dim nextRun = If(uiConfig?.LastRunTimestamp, DateTime.Now).AddMilliseconds(updateTimer.Interval)
        Dim diff = nextRun - DateTime.Now
        If diff.TotalSeconds <= 0 Then
            lblCountdown.Text = "Updating now..."
            If diff.TotalSeconds < -5 Then UpdateAllAsync(True)
        Else
            lblCountdown.Text = String.Format("Next run in: {0} min {1} sec", CInt(Math.Floor(diff.TotalMinutes)), diff.Seconds)
        End If
    End Sub

    Private Sub UpdateTrayStatus()
        If notify Is Nothing Then Return
        notify.Icon = CreateColoredIcon(If(Not updateTimer.Enabled, Color.Red, If(lastUpdateAllSuccess, Color.Green, Color.Yellow)))
    End Sub

    Private Function CreateColoredIcon(c As Color) As Icon
        Dim bmp As New Bitmap(16, 16)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent) : Using b As New SolidBrush(c) : g.FillEllipse(b, 0, 0, 15, 15) : End Using
        End Using
        Dim hIcon = bmp.GetHicon()
        Dim ico = Icon.FromHandle(hIcon)
        Return CType(ico.Clone(), Icon)
    End Function

    Private Async Sub ChkEnableTimer_CheckedChanged(sender As Object, e As EventArgs)
        If chkEnableTimer.Checked AndAlso uiConfig IsNot Nothing AndAlso Not uiConfig.LastRunTimestamp.HasValue Then
            uiConfig.LastRunTimestamp = DateTime.Now : SaveUiConfig(uiConfig)
        End If
        Await SaveConfigAsync() : InitializeTimer()
    End Sub

    Private Async Sub NumInterval_ValueChanged(sender As Object, e As EventArgs)
        If numInterval.Value < 5 Then numInterval.Value = 5
        Await SaveConfigAsync() : InitializeTimer()
    End Sub

    Private Function LoadUiConfig() As UIConfig
        Try : If File.Exists(UiConfigPath) Then Return JsonSerializer.Deserialize(Of UIConfig)(File.ReadAllText(UiConfigPath))
        Catch : End Try : Return New UIConfig()
    End Function

    Private Sub SaveUiConfig(cfg As UIConfig)
        File.WriteAllText(UiConfigPath, JsonSerializer.Serialize(cfg, New JsonSerializerOptions() With {.WriteIndented = True}))
    End Sub

    Private Sub ApplyTheme(cfg As UIConfig)
        Try
            Dim back = If(cfg.IsDarkMode, Color.FromArgb(20, 20, 20), Color.WhiteSmoke)
            Dim fore = If(cfg.IsDarkMode, Color.White, Color.Black)
            Me.BackColor = back : Me.ForeColor = fore

            rtbLog.BackColor = If(cfg.IsDarkMode, Color.Black, Color.White)
            rtbLog.ForeColor = If(cfg.IsDarkMode, Color.Lime, Color.DarkGreen)

            ' Recursively apply theme to all nested controls so FlowLayoutPanel children are handled
            ApplyThemeToControl(Me, back, fore, cfg)

            ' Specific chkStartup check just in case it wasn't in a panel (though it is now)
            If chkStartup IsNot Nothing Then
                chkStartup.ForeColor = If(cfg.IsDarkMode, Color.White, Color.Black)
                chkStartup.BackColor = Color.Transparent
            End If

            If dgvRecords IsNot Nothing Then
                dgvRecords.BackgroundColor = If(cfg.IsDarkMode, Color.FromArgb(30, 30, 30), Color.LightGray)
                dgvRecords.DefaultCellStyle.BackColor = If(cfg.IsDarkMode, Color.FromArgb(45, 45, 48), Color.White)
                dgvRecords.DefaultCellStyle.ForeColor = If(cfg.IsDarkMode, Color.White, Color.Black)
                dgvRecords.EnableHeadersVisualStyles = False
            End If

        Catch : End Try
    End Sub

    Private Sub ApplyThemeToControl(ctrl As Control, back As Color, fore As Color, cfg As UIConfig)
        Try
            ctrl.BackColor = back
            ctrl.ForeColor = fore
            For Each child As Control In ctrl.Controls
                If TypeOf child Is Label OrElse TypeOf child Is CheckBox Then
                    child.ForeColor = fore
                    child.BackColor = Color.Transparent
                ElseIf TypeOf child Is TextBox Then
                    child.BackColor = If(cfg.IsDarkMode, Color.FromArgb(30, 30, 30), Color.White)
                    child.ForeColor = fore
                ElseIf TypeOf child Is Button Then
                    child.BackColor = If(cfg.IsDarkMode, Color.FromArgb(45, 45, 48), Color.LightGray)
                    child.ForeColor = fore
                    child.Margin = New Padding(4)
                Else
                    ' For container controls (Panel, FlowLayoutPanel, TableLayoutPanel, etc), recurse
                    ApplyThemeToControl(child, back, fore, cfg)
                End If
            Next
        Catch : End Try
    End Sub

    Private Sub ChkDark_CheckedChanged(sender As Object, e As EventArgs)
        uiConfig.IsDarkMode = DirectCast(sender, CheckBox).Checked
        ApplyTheme(uiConfig) : SaveUiConfig(uiConfig)
    End Sub

    Private Sub ChkStartup_CheckedChanged(sender As Object, e As EventArgs)
        Try
            uiConfig.RunAtStartup = DirectCast(sender, CheckBox).Checked
            SetStartup(uiConfig.RunAtStartup)
            SaveUiConfig(uiConfig)
        Catch ex As Exception
            Log("Startup toggle error: " & ex.Message)
        End Try
    End Sub

    Private Sub SetStartup(ByVal enable As Boolean)
        Try
            Dim key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\Microsoft\Windows\CurrentVersion\Run", True)
            If enable Then
                key.SetValue(Application.ProductName, """" & Application.ExecutablePath & """")
            Else
                key.DeleteValue(Application.ProductName, False)
            End If
        Catch ex As Exception
            Log("Registry Error: " & ex.Message)
        End Try
    End Sub

    Private Async Function SaveConfigAsync() As Task
        Dim cfg As New DdnsConfig() With {.Records = records.ToList(), .PeriodicEnabled = chkEnableTimer.Checked, .PeriodicIntervalMinutes = CInt(numInterval.Value)}
        Await File.WriteAllTextAsync(ConfigFilePath, JsonSerializer.Serialize(cfg, New JsonSerializerOptions() With {.WriteIndented = True}))
    End Function

    Private Async Function UpdateAllAsync(Optional isAutomated As Boolean = False) As Task
        If records Is Nothing OrElse records.Count = 0 Then Return

        ' 1. Attempt to get current REAL Public IP
        Dim currentPublicIp As String = Await GetPublicIpAsync()
        Dim isFallbackMode As Boolean = String.IsNullOrEmpty(currentPublicIp)

        If isFallbackMode Then
            ' BIG VISUAL WARNING
            Log("!!! CLOUDFLARE IP LOOKUP FAILED !!!", Color.OrangeRed, True)
            Log("FALLING BACK TO NAMECHEAP AUTO-DETECTION", Color.Yellow, True)
            lblDetectedIp.Text = "My Public IP: FAILED (Auto-detecting...)"
        Else
            lblDetectedIp.Text = "My Public IP: " & currentPublicIp
        End If

        uiConfig.LastRunTimestamp = DateTime.Now
        SaveUiConfig(uiConfig)

        Dim allSuccess As Boolean = True
        Using client As New HttpClient()
            For Each r In records
                Try
                    ' 2. Check DNS Comparison (Only possible if we have a detected IP)
                    Dim fullDomain = If(r.Host = "@", r.Domain, $"{r.Host}.{r.Domain}")
                    Dim currentDnsIp As String = ""

                    Try
                        Dim dnsIps = Await System.Net.Dns.GetHostAddressesAsync(fullDomain)
                        currentDnsIp = dnsIps.FirstOrDefault()?.ToString()
                        lblDomainIp.Text = "DNS points to: " & If(String.IsNullOrEmpty(currentDnsIp), "Unresolved", currentDnsIp)
                    Catch
                        lblDomainIp.Text = "DNS points to: Error"
                    End Try

                    ' 3. Decide whether to update
                    ' In Fallback mode, we ALWAYS update because we can't verify the IP match
                    If isFallbackMode OrElse Not isAutomated OrElse currentDnsIp <> currentPublicIp Then

                        ' Construct URL: If we have an IP, send it. If not, omit it for auto-detect.
                        Dim url = $"https://dynamicdns.park-your-domain.com/update?host={Uri.EscapeDataString(r.Host)}&domain={Uri.EscapeDataString(r.Domain)}&password={Uri.EscapeDataString(r.Password)}"
                        If Not isFallbackMode Then url &= $"&ip={currentPublicIp}"

                        Dim body = Await client.GetStringAsync(url)
                        Dim result = ParseNamecheapResponse(body)

                        r.LastStatus = result : r.LastUpdated = DateTime.Now

                        If result.StartsWith("Success") Then
                            Log($"{r.Host}.{r.Domain} -> {result}", If(isFallbackMode, Color.Cyan, Color.Lime))
                        Else
                            allSuccess = False
                            Log($"{r.Host}.{r.Domain} -> {result}", Color.Red)
                        End If
                    Else
                        r.LastStatus = "Verified (No Change)"
                        r.LastUpdated = DateTime.Now
                        Log($"{fullDomain} is already up to date ({currentPublicIp}).")
                    End If
                Catch ex As Exception
                    allSuccess = False
                    Log($"Update Error {r.Domain}: {ex.Message}", Color.Red)
                End Try
            Next
        End Using

        lastUpdateAllSuccess = allSuccess
        UpdateTrayStatus()
        lblLastRunStatus.Text = $"Last run: {DateTime.Now:HH:mm:ss} - {(If(allSuccess, "Success", "Fail"))}"
        RefreshRecordList()
    End Function

    ' Enhanced Log for High Visibility
    Private Sub Log(msg As String, Optional c As Color = Nothing, Optional isBold As Boolean = False)
        If c = Nothing Then c = If(uiConfig.IsDarkMode, Color.Lime, Color.DarkGreen)

        rtbLog.SelectionStart = rtbLog.TextLength
        rtbLog.SelectionLength = 0
        rtbLog.SelectionColor = c

        If isBold Then
            rtbLog.SelectionFont = New Font(rtbLog.Font.FontFamily, 12.0F, FontStyle.Bold)
        Else
            rtbLog.SelectionFont = rtbLog.Font
        End If

        rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}")
        rtbLog.SelectionColor = rtbLog.ForeColor ' Reset color
        rtbLog.ScrollToCaret()
    End Sub

    Private Async Function GetPublicIpAsync() As Task(Of String)
        Try
            Using client As New HttpClient()
                Dim ip = Await client.GetStringAsync("https://ipv4.icanhazip.com") ' We need IPV4 specifically because Namecheap doesn't support IPv6 for DDNS.
                Return ip.Trim()
            End Using
        Catch : Return String.Empty : End Try
    End Function

    Private Function ParseNamecheapResponse(xml As String) As String
        Try
            Dim doc As New System.Xml.XmlDocument() : doc.LoadXml(xml)
            If doc.SelectSingleNode("//ErrCount").InnerText = "0" Then Return "Success IP=" & doc.SelectSingleNode("//IP").InnerText
            Return "Error: " & doc.SelectSingleNode("//Err1").InnerText
        Catch : Return "Invalid Response" : End Try
    End Function
End Class