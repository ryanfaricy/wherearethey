using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Radzen;
using WhereAreThey.Components;

namespace WhereAreThey.Tests.Components;

public abstract class ComponentTestBase : BunitContext
{
    protected Mock<IStringLocalizer<App>> LocalizerMock { get; private set; }

    protected ComponentTestBase()
    {
        LocalizerMock = new Mock<IStringLocalizer<App>>();
        
        // Default behavior for localizer: return the key itself
        LocalizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));
        
        Services.AddSingleton(LocalizerMock.Object);
        
        // Add Radzen services
        Services.AddRadzenComponents();
        
        // Add JSInterop mock
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
