using Microsoft.AspNetCore.Mvc;
using test_rest.Models;

namespace test_rest.Controllers;

// =========================================================================
// TEORIA SU REST API, CONTROLLER E STATUS CODE HTTP
// =========================================================================
// [ApiController]: abilita comportamenti automatici fondamentali per le Web API:
//   1. Validazione automatica dei DTO (se un campo [Required] manca, risponde subito 400 Bad Request).
//   2. Inferenza automatica della sorgente dati ([FromBody] per i JSON nel body, [FromRoute] per l'URL, [FromQuery] per ?param=...).
//   3. Risposte di errore standardizzate (ProblemDetails RFC 7807).
//
// [Route("api/[controller]")]: mappa l'URL base a "/api/tickets" (prende il nome della classe senza "Controller").
//
// RIEPILOGO VERBI HTTP E STATUS CODE DEL CRUD:
// - GET    (Lettura)               -> 200 OK (o 404 Not Found se l'ID non esiste)
// - POST   (Creazione)             -> 201 Created + Header 'Location' (o 400 Bad Request se input non valido)
// - PUT    (Modifica COMPLETA)     -> 200 OK oppure 204 No Content (o 404 Not Found)
// - PATCH  (Modifica PARZIALE)     -> 200 OK oppure 204 No Content (o 404 Not Found)
// - DELETE (Eliminazione)          -> 204 No Content (o 404 Not Found)
// =========================================================================

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    // Simuliamo un database in memoria (in un progetto reale inietteresti qui il DbContext o un IRepository tramite costruttore)
    private static readonly List<TicketItem> Database =
    [
        new TicketItem { Id = 1, Code = "INC-001", Title = "Portale non disponibile", Description = "Errore 500 al login", Priority = 1, IsClosed = false },
        new TicketItem { Id = 2, Code = "REQ-001", Title = "Nuova utenza VPN", Description = "Attivare accesso per consulente", Priority = 2, IsClosed = true },
        new TicketItem { Id = 3, Code = "INC-002", Title = "Display LED spento", Description = "Pannello linea 4 non raggiungibile", Priority = 1, IsClosed = false }
    ];

    // =====================================================================
    // 1. READ ALL (GET /api/tickets?isClosed=false&priority=1)
    // Supporta anche filtri opzionali in Query String ([FromQuery]).
    // Restituisce: 200 OK con la lista di TicketResponseDto.
    // =====================================================================
    [HttpGet]
    public ActionResult<IEnumerable<TicketResponseDto>> GetAll(
        [FromQuery] bool? isClosed,
        [FromQuery] int? priority)
    {
        IEnumerable<TicketItem> query = Database;

        if (isClosed.HasValue)
        {
            query = query.Where(t => t.IsClosed == isClosed.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        var result = query
            .OrderBy(t => t.Priority)
            .Select(MapToDto)
            .ToList();

        return Ok(result); // HTTP 200 OK
    }

    // =====================================================================
    // 2. READ BY ID (GET /api/tickets/1)
    // {id:int} è un Route Constraint: accetta solo numeri interi nell'URL.
    // Restituisce: 200 OK se trovato, oppure 404 Not Found se non esiste.
    // =====================================================================
    [HttpGet("{id:int}")]
    public ActionResult<TicketResponseDto> GetById([FromRoute] int id)
    {
        TicketItem? ticket = Database.FirstOrDefault(t => t.Id == id);

        if (ticket is null)
        {
            // HTTP 404 Not Found con messaggio JSON esplicativo
            return NotFound(new { Message = $"Ticket con ID {id} non trovato." });
        }

        return Ok(MapToDto(ticket)); // HTTP 200 OK
    }

    // =====================================================================
    // 3. CREATE (POST /api/tickets)
    // Riceve il JSON nel body della richiesta ([FromBody] implicito grazie a [ApiController]).
    // DOMANDA DA COLLOQUIO: "Cosa deve restituire una POST che crea una risorsa?"
    // Deve restituire HTTP 201 Created (tramite CreatedAtAction), che include:
    // - L'header HTTP 'Location' con l'URL per leggere la nuova risorsa (es. /api/tickets/4)
    // - Il body JSON con l'oggetto appena creato (incluso il nuovo Id).
    // =====================================================================
    [HttpPost]
    public ActionResult<TicketResponseDto> Create([FromBody] CreateTicketDto input)
    {
        // Controllo di business: verifichiamo se esiste già un ticket con lo stesso codice
        if (Database.Any(t => t.Code.Equals(input.Code, StringComparison.OrdinalIgnoreCase)))
        {
            // HTTP 409 Conflict (oppure 400 BadRequest) quando c'è un conflitto di unicità
            return Conflict(new { Message = $"Esiste già un ticket con codice '{input.Code}'." });
        }

        int nextId = Database.Count > 0 ? Database.Max(t => t.Id) + 1 : 1;

        var newTicket = new TicketItem
        {
            Id = nextId,
            Code = input.Code.Trim().ToUpperInvariant(),
            Title = input.Title.Trim(),
            Description = input.Description.Trim(),
            Priority = input.Priority,
            IsClosed = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Database.Add(newTicket);

        TicketResponseDto responseDto = MapToDto(newTicket);

        // HTTP 201 Created: punta all'azione GetById passando il nuovo id
        return CreatedAtAction(
            actionName: nameof(GetById),
            routeValues: new { id = newTicket.Id },
            value: responseDto);
    }

    // =====================================================================
    // 4. UPDATE COMPLETO (PUT /api/tickets/1)
    // Sostituisce tutti i campi modificabili della risorsa identificata da {id}.
    // Restituisce: 200 OK (con l'oggetto aggiornato) o 204 No Content, oppure 404 Not Found.
    // =====================================================================
    [HttpPut("{id:int}")]
    public ActionResult<TicketResponseDto> Update([FromRoute] int id, [FromBody] UpdateTicketDto input)
    {
        TicketItem? existing = Database.FirstOrDefault(t => t.Id == id);

        if (existing is null)
        {
            return NotFound(new { Message = $"Ticket con ID {id} non trovato." });
        }

        existing.Title = input.Title.Trim();
        existing.Description = input.Description.Trim();
        existing.Priority = input.Priority;
        existing.IsClosed = input.IsClosed;

        return Ok(MapToDto(existing)); // HTTP 200 OK
    }

    // =====================================================================
    // 5. UPDATE PARZIALE (PATCH /api/tickets/1/close)
    // Differenza tra PUT e PATCH (Domanda classica da colloquio!):
    // - PUT invia e sovrascrive l'intera risorsa.
    // - PATCH modifica solo uno specifico campo o stato (qui chiude il ticket).
    // =====================================================================
    [HttpPatch("{id:int}/close")]
    public ActionResult<TicketResponseDto> CloseTicket([FromRoute] int id)
    {
        TicketItem? existing = Database.FirstOrDefault(t => t.Id == id);

        if (existing is null)
        {
            return NotFound(new { Message = $"Ticket con ID {id} non trovato." });
        }

        existing.IsClosed = true;
        return Ok(MapToDto(existing)); // HTTP 200 OK
    }

    // =====================================================================
    // 6. DELETE (DELETE /api/tickets/1)
    // Rimuove la risorsa.
    // Restituisce: 204 No Content (operazione riuscita, nessun body da restituire) o 404 Not Found.
    // =====================================================================
    [HttpDelete("{id:int}")]
    public IActionResult Delete([FromRoute] int id)
    {
        TicketItem? existing = Database.FirstOrDefault(t => t.Id == id);

        if (existing is null)
        {
            return NotFound(new { Message = $"Ticket con ID {id} non trovato." });
        }

        Database.Remove(existing);

        return NoContent(); // HTTP 204 No Content
    }

    // Metodo helper privato per convertire l'Entità interna nel DTO di risposta
    private static TicketResponseDto MapToDto(TicketItem t) =>
        new(t.Id, t.Code, t.Title, t.Description, t.Priority, t.IsClosed, t.CreatedAt);
}
