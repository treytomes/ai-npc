namespace Adventure;

class CommandLineProps
{
	public required string ConfigFile { get; set; }
	public bool Debug { get; set; }
	public bool Fullscreen { get; set; }
	public int? Width { get; set; }
	public int? Height { get; set; }
}