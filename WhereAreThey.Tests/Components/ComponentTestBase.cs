using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Moq;
using Radzen;
using WhereAreThey.Components;
using WhereAreThey.Services.Interfaces;
using WhereAreThey.Models;

namespace WhereAreThey.Tests.Components;

public abstract class ComponentTestBase : BunitContext
{
    protected Mock<IStringLocalizer<App>> LocalizerMock { get; private set; }
    protected Mock<IValidationService> ValidationServiceMock { get; private set; }

    protected ComponentTestBase()
    {
        LocalizerMock = new Mock<IStringLocalizer<App>>();
        
        // Default behavior for localizer: return the key itself
        LocalizerMock.Setup(l => l[It.IsAny<string>()]).Returns((string name) => new LocalizedString(name, name));
        
        Services.AddSingleton(LocalizerMock.Object);

        ValidationServiceMock = new Mock<IValidationService>();
        // Default behavior for validation service: execute the operation and return success
        ValidationServiceMock.Setup(v => v.ExecuteAsync(It.IsAny<Func<Task<Result>>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Func<Task>>(), It.IsAny<Func<string, Task>>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns<Func<Task<Result>>, string, string, Func<Task>, Func<string, Task>, bool, bool, bool, string>(async (op, _, _, success, failure, _, _, haptic, _) => {
                var result = await op();
                if (result.IsSuccess) {
                    if (success != null)
                    {
                        await success();
                    }

                    if (!haptic)
                    {
                        return result.IsSuccess;
                    }

                    var hapticService = Services.GetService<IHapticFeedbackService>();
                    if (hapticService != null)
                    {
                        await hapticService.VibrateSuccessAsync();
                    }
                } else {
                    if (failure != null)
                    {
                        await failure(result.Error ?? "Error");
                    }

                    if (!haptic)
                    {
                        return result.IsSuccess;
                    }

                    var hapticService = Services.GetService<IHapticFeedbackService>();
                    if (hapticService != null)
                    {
                        await hapticService.VibrateErrorAsync();
                    }
                }
                return result.IsSuccess;
            });

        Services.AddSingleton(ValidationServiceMock.Object);
        
        // Add Radzen services
        Services.AddRadzenComponents();
        
        // Add JSInterop mock
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
