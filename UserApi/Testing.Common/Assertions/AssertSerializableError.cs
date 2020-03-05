using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Testing.Common.Assertions
{
    public static class AssertSerializableError
    {
        public static void ContainsKeyAndErrorMessage(this SerializableError error, string key, string errorMessage)
        {
            error.Should().NotBeNull();
            error.ContainsKey(key).Should().BeTrue();
            ((string[])error[key])[0].Should().Be(errorMessage);
        }

        public static void ContainsKeyAndErrorMessage(this ModelStateDictionary model, string key, string errorMessage)
        {
            model.Should().NotBeNull();
            model.ContainsKey(key).Should().BeTrue();
            model[key].Errors[0].ErrorMessage.Should().Be(errorMessage);
        }
    }
}
