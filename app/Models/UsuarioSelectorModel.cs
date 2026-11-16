namespace Controlex.Web.Models;

public record OpcionUsuario(int Id, string Texto);

public class SelectorUsuariosViewModel
{
    public string Id { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Etiqueta { get; set; } = "";
    public string Placeholder { get; set; } = "";
    public string MensajeSinOpciones { get; set; } = "";
    public string? UrlUsuarios { get; set; }
    public string? IdProyectoSelectId { get; set; }
    public List<OpcionUsuario> Opciones { get; set; } = new();
    public List<int> Seleccionados { get; set; } = new();
}
