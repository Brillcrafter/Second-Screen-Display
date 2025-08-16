using System;
using System.Collections.Generic;
using System.Threading;
using NLog;
using ImGuiNET;


namespace ClientPlugin.ImGui;

public sealed class RenderHandler : IRootRenderComponent
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static RenderHandler? _current;
    private static IGuiHandler? _guiHandler;
    public static RenderHandler Current => _current ?? throw new InvalidOperationException("Render is not yet initialized");
    public static IGuiHandler GuiHandler => _guiHandler ?? throw new InvalidOperationException("Render is not yet initialized");

    private readonly List<ComponentRegistration> _components = [];
    private readonly Lock _componentsLock = new();

    internal RenderHandler(IGuiHandler guiHandler)
    {
        _current = this;
        _guiHandler = guiHandler;
    }

    public void RegisterComponent<TComponent>(TComponent instance) where TComponent : IRenderComponent
    {
        lock (_componentsLock)
            _components.Add(new ComponentRegistration(typeof(TComponent), instance));
    }

    public void UnregisterComponent<TComponent>(TComponent instance) where TComponent : IRenderComponent
    {
        lock (_componentsLock)
        {
            for (var i = 0; i < _components.Count; i++)
            {
                var (instanceType, renderComponent) = _components[i];
                if (renderComponent.Equals(instance) && instanceType == typeof(TComponent))
                {
                    _components.RemoveAtFast(i);
                    return;
                }
            }
        }
    }

    void IRenderComponent.OnFrame()
    {
#if DEBUG
        ImGuiNET.ImGui.ShowDemoWindow();
#endif

        lock (_componentsLock)
        {
            foreach (var (instanceType, renderComponent) in _components)
            {
                try
                {
                    renderComponent.OnFrame();
                }
                catch (Exception e)
                {
                    Log.Error(e, "Component {TypeName} failed to render a new frame", instanceType);
                }
            }
        }
    }

    private record ComponentRegistration(Type InstanceType, IRenderComponent Instance);

    public void Dispose()
    {
        _current = null;
        _components.Clear();
    }
}