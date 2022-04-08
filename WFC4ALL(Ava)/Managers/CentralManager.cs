﻿using WFC4ALL.ViewModels;
using WFC4ALL.Views;

namespace WFC4ALL.Managers;

public class CentralManager {
    private readonly InputManager inputManager;
    private readonly UIManager uiManager;
    private readonly WFCHandler wfcHandler;

    private readonly MainWindow mainWindow;
    private readonly PaintingWindow paintingWindow;
    private readonly MainWindowViewModel mainWindowViewModel;

    public CentralManager(MainWindowViewModel mWVM, MainWindow mW, PaintingWindow pW) {
        mainWindow = mW;
        paintingWindow = pW;
        mainWindowViewModel = mWVM;
        
        wfcHandler = new WFCHandler(this);
        uiManager = new UIManager(this);
        inputManager = new InputManager( this);
        
    }

    public InputManager getInputManager() {
        return inputManager;
    }

    public UIManager getUIManager() {
        return uiManager;
    }

    public WFCHandler getWFCHandler() {
        return wfcHandler;
    }

    public MainWindow getMainWindow() {
        return mainWindow;
    }

    public PaintingWindow getPaintingWindow() {
        return paintingWindow;
    }

    public MainWindowViewModel getMainWindowVM() {
        return mainWindowViewModel;
    }
}