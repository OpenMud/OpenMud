using SadConsole.Input;
using SadConsole.UI;
using SadConsole.UI.Controls;
using SadRogue.Primitives;

namespace OpenMud.Mudpiler.Client.Terminal.UI;

public delegate void SubmitCommand(string command);

public class InteractionWindow : Window
{
    private ListBox logList;
    private TextBox txtInput;

    public InteractionWindow(int width, int height) : base(width, height)
    {
        DefaultBackground = Color.AnsiRed;
        Title = "Event Log";
        IsVisible = true;
        CanDrag = false;

        logList = new ListBox(150, 6);
        logList.Position = new Point(5, 1);

        txtInput = new TextBox(100);
        txtInput.Position = new Point(5, 8);
        Controls.Add(txtInput);
        Controls.Add(logList);

        var btnSubmit = new Button(40);
        btnSubmit.Position = new Point(115, 8);
        btnSubmit.Text = "Submit";

        Controls.Add(btnSubmit);
        Controls.Add(txtInput);
        Controls.Add(logList);

        btnSubmit.Click += OnSubmit;
        txtInput.KeyPressed += OnCommandInput;
    }

    public event SubmitCommand? OnSubmitCommand;

    private void OnCommandInput(object? sender, TextBox.KeyPressEventArgs e)
    {
        if (e.Key.Key == Keys.Enter)
            DoSubmit();
    }

    private void DoSubmit()
    {
        var cmd = txtInput.Text;

        txtInput.Text = "";

        if (OnSubmitCommand != null)
            OnSubmitCommand(cmd);
    }

    private void OnSubmit(object? sender, EventArgs e)
    {
        DoSubmit();
    }

    public void SetCommandText(string text)
    {
        txtInput.Text = text;
    }

    public void RecordLog(string message)
    {
        logList.Items.Add(message);
        logList.ScrollBar.Value = logList.ScrollBar.Maximum;
    }
}