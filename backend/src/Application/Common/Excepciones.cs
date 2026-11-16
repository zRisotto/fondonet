namespace FondoNet.Application.Common;

/// <summary>Violación de una regla de negocio (la API la traduce a 400/422).</summary>
public class ReglaNegocioException(string mensaje) : Exception(mensaje);

/// <summary>Recurso inexistente (la API la traduce a 404).</summary>
public class NoEncontradoException(string mensaje) : Exception(mensaje);
