namespace BlazorApp.Components.Models
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public Address Address { get; set; } = new();
    }
}
