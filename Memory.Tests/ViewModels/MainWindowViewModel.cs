using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Controls;

namespace Memory.Tests.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private bool _isInitialized;
    private readonly Mem _mem = new();
    
    [ObservableProperty] 
    private string _applicationTitle = string.Empty;

    #region Open Process Variables

    [ObservableProperty] 
    private string _openProcessTargetName = string.Empty;
    
    [ObservableProperty] 
    private string _openProcessResponse = string.Empty;
    
    [ObservableProperty] 
    private bool _isProcessOpen;

    #endregion

    #region Signature Scan Variables

    [ObservableProperty]
    private string _signature = string.Empty;

    [ObservableProperty]
    private string _module = string.Empty;

    [ObservableProperty]
    private string _signatureScanResult = "0";
    
    #endregion
    
    #region Memory Writes Variables

    [Flags]
    public enum WriteTypes
    {
        Byte,
        Short,
        Int,
        Float,
        Double,
        String
    }
    
    [ObservableProperty]
    private string _writeAddress = string.Empty;

    [ObservableProperty]
    private WriteTypes _writeType;
    
    [ObservableProperty]
    private int _writeIndex;

    [ObservableProperty]
    private string _writeValue = "0";
    
    #endregion
    
    public MainWindowViewModel()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }
    
    private void InitializeViewModel()
    {
        ApplicationTitle = "Memory.dll Test App";
        _isInitialized = true;
    }

    [RelayCommand]
    private void ButtonAction(string parameter)
    {
        switch (parameter)
        {
            case "attach":
            {
                AttachToProcess();
                break;
            }
            case "sig":
            {
                SignatureScan();
                break;
            }
            case "write":
            {
                Write();
                break;
            }
            case "read":
            {
                break;
            }
            case "protection":
            {
                break;
            }
        }
    }

    private void AttachToProcess()
    {
        var result = _mem.OpenProcess(OpenProcessTargetName);
        if (result == Mem.OpenProcessResults.Success)
        {
            Task.Run(MonitorIfProcessIsStillOpen);
        }        
        
        OpenProcessResponse = $"Open Process Response: {result}";
    }

    private async void MonitorIfProcessIsStillOpen()
    {
        IsProcessOpen = true;

        while (_mem.OpenProcess(OpenProcessTargetName) == Mem.OpenProcessResults.Success)
        {
            await Task.Delay(100);
        }

        IsProcessOpen = false;
    }

    private void SignatureScan()
    {
        if (string.IsNullOrWhiteSpace(Signature))
        {
            const string error = "Signature cant be null, empty or whitespace";
            ShowError(error);
            return;
        }

        var module = "default";
        if (!string.IsNullOrWhiteSpace(Module))
        {
            module = Module;
        }

        SignatureScanResult = _mem.ScanForSig(Signature, 1, module: module).FirstOrDefault().ToString("X");
    }

    private void Write()
    {
        if (string.IsNullOrWhiteSpace(WriteAddress))
        {
            const string error = "Address cant be null, empty or whitespace";
            ShowError(error);
            return;
        }

        if (!nuint.TryParse(WriteAddress, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var address))
        {
            const string error = "Failed to parse the write address";
            ShowError(error);
            return;
        }

        WriteType = (WriteTypes)WriteIndex;
        switch (WriteType)
        {
            case WriteTypes.Byte:
            {
                if (!byte.TryParse(WriteValue, out var result))
                {
                    const string error = "Failed to parse the value (type: Byte)";
                    ShowError(error);
                    return;
                }
                
                _mem.WriteMemory(address, result);
                break;
            }
            case WriteTypes.Short:
            {
                if (!short.TryParse(WriteValue, out var result))
                {
                    const string error = "Failed to parse the value (type: Short)";
                    ShowError(error);
                    return;
                }
                
                _mem.WriteMemory(address, result);
                break;
            }
            case WriteTypes.Int:
            {
                if (!int.TryParse(WriteValue, out var result))
                {
                    const string error = "Failed to parse the value (type: Int)";
                    ShowError(error);
                    return;
                }
                
                _mem.WriteMemory(address, result);
                break;
            }
            case WriteTypes.Float:
            {
                if (!float.TryParse(WriteValue, out var result))
                {
                    const string error = "Failed to parse the value (type: Float)";
                    ShowError(error);
                    return;
                }
                
                _mem.WriteMemory(address, result);
                break;
            }
            case WriteTypes.Double:
            {
                if (!double.TryParse(WriteValue, out var result))
                {
                    const string error = "Failed to parse the value (type: Double)";
                    ShowError(error);
                    return;
                }
                
                _mem.WriteMemory(address, result);
                break;
            }
            case WriteTypes.String:
            {
                _mem.WriteStringMemory(address, WriteValue);
                break;
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private static async void ShowError(string error)
    {
        var messageBox = new MessageBox
        {
            Title = "Error",
            Content = error
        };
        await messageBox.ShowDialogAsync();
    }
}