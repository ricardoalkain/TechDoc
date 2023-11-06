using Microsoft.AspNetCore.Mvc;
using TechDoc.Data.Exceptions;

namespace TechDoc.Api.Controllers
{
    public class ApiControllerBase : ControllerBase
    {
        protected IActionResult GetResponse<T>(Func<T> function)
        {
            try
            {
                var response = function();
                if (response is string rawText)
                {
                    return Content(rawText);
                }

                return Ok(response);
            }
            catch(DocumentNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        protected async Task<IActionResult> GetResponse<T>(Func<Task<T>> function)
        {
            try
            {
                var response = await function();
                if (response is string rawText)
                {
                    return Content(rawText);
                }

                return Ok(response);
            }
            catch(DocumentNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        protected IActionResult GetResponse(Action method)
        {
            try
            {
                method();
                return Ok();
            }
            catch(DocumentNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        protected async Task<IActionResult> GetResponse(Func<Task> method)
        {
            try
            {
                await method();
                return Ok();
            }
            catch(DocumentNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UserException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
