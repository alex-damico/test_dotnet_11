using System.ComponentModel.DataAnnotations;

namespace test_rest.Models;

// =========================================================================
// TEORIA: PERCHÉ USIAMO I DTO (Data Transfer Object) INVECE DELLE ENTITÀ DB?
// =========================================================================
// DOMANDA DA COLLOQUIO: "Perché in un'API REST non esponiamo direttamente la classe Entità del DB?"
// 1. Sicurezza (Over-posting / Mass Assignment): un utente malintenzionato potrebbe inviare nel JSON
//    campi che non dovrebbe modificare (es. "isAdmin": true o "id": 999).
// 2. Disaccoppiamento: se rinomini una colonna sul Database, non rompi il contratto JSON con il Frontend!
// 3. Controllo dei dati: nel POST non chiediamo l'Id (lo genera il server), mentre nel GET restituiamo l'Id.
// =========================================================================

// Modello interno salvato a database (Entità)
public class TicketItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// DTO di RISPOSTA (usato nelle GET e POST per restituire i dati al client)
public record TicketResponseDto(
    int Id,
    string Code,
    string Title,
    string Description,
    int Priority,
    bool IsClosed,
    DateTimeOffset CreatedAt);

// DTO per la CREAZIONE (usato nel POST: non contiene Id né IsClosed)
// Le DataAnnotations ([Required], [Range], [StringLength]) vengono validate AUTOMATICAMENTE
// da ASP.NET Core grazie all'attributo [ApiController] sul Controller (restituendo 400 Bad Request se non valide!).
public record CreateTicketDto(
    [Required(ErrorMessage = "Il codice è obbligatorio.")]
    [StringLength(20, MinimumLength = 3)]
    string Code,

    [Required(ErrorMessage = "Il titolo è obbligatorio.")]
    string Title,

    string Description,

    [Range(1, 3, ErrorMessage = "La priorità deve essere compresa tra 1 (Alta) e 3 (Bassa).")]
    int Priority);

// DTO per l'AGGIORNAMENTO COMPLETO (usato nel PUT)
public record UpdateTicketDto(
    [Required] string Title,
    string Description,
    [Range(1, 3)] int Priority,
    bool IsClosed);
