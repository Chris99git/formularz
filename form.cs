using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CandidateApplicationForm.Pages
{
    public class IndexModel : PageModel
    {
        private readonly string _allowedFileExtensions = ".jpg,.pdf,.doc";

        [BindProperty]
        public CandidateFormModel CandidateForm { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Zapisanie danych w bazie danych
            SaveCandidateFormToDatabase(CandidateForm);

            // Zapisanie załączników na serwerze
            SaveAttachments();

            return RedirectToPage("ThankYou");
        }

        private void SaveCandidateFormToDatabase(CandidateFormModel candidateForm)
        {
            // Kod do zapisu danych w bazie danych            
            // Tutaj znajdowałoby się wykonanie odpowiednich operacji na bazie danych
        }

        private void SaveAttachments()
        {
            var attachments = new List<IFormFile>
            {
                CandidateForm.Attachment1,
                CandidateForm.Attachment2,
            };

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "Attachments");

            foreach (var attachment in attachments.Where(a => a != null))
            {
                var fileExtension = Path.GetExtension(attachment.FileName);
                if (!IsAllowedFileExtension(fileExtension))
                {
                    ModelState.AddModelError("CandidateForm.Attachment1", "Nieprawidłowy format pliku.");
                    continue;
                }

                var uniqueFileName = Guid.NewGuid().ToString("N") + fileExtension;
                var filePath = Path.Combine(uploadPath, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    attachment.CopyTo(fileStream);
                }
            }
        }

        private bool IsAllowedFileExtension(string fileExtension)
        {
            return _allowedFileExtensions.Split(',').Contains(fileExtension.ToLower());
        }
    }

    public class CandidateFormModel
    {
        [Required(ErrorMessage = "Pole Imię jest wymagane.")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Pole Nazwisko jest wymagane.")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Pole Data urodzenia jest wymagane.")]
        [RegularExpression(@"\d{4}-\d{2}-\d{2}", ErrorMessage = "Nieprawidłowy format daty.")]
        public string DateOfBirth { get; set; }

        [Required(ErrorMessage = "Pole Adres e-mail jest wymagane.")]
        [EmailAddress(ErrorMessage = "Nieprawidłowy format adresu e-mail.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Pole Wykształcenie jest wymagane.")]
        public string Education { get; set; }

        [AllowedExtensions(".jpg,.pdf,.doc", ErrorMessage = "Nieprawidłowy format pliku.")]
        public IFormFile Attachment1 { get; set; }

        [AllowedExtensions(".jpg,.pdf,.doc", ErrorMessage = "Nieprawidłowy format pliku.")]
        public IFormFile Attachment2 { get; set; }
    }

    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string _allowedExtensions;

        public AllowedExtensionsAttribute(string allowedExtensions)
        {
            _allowedExtensions = allowedExtensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var file = value as IFormFile;

            if (file != null)
            {
                var fileExtension = Path.GetExtension(file.FileName);
                if (!_allowedExtensions.Split(',').Contains(fileExtension.ToLower()))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
