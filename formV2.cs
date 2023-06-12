using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Web;
using Grpc.Core;

namespace Project1
{
    public class ApplicantForm
    {
        // Dane osobowe
        [Required(ErrorMessage = "Imię jest wymagane")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Pole Data urodzenia jest wymagane.")]
        [RegularExpression(@"(19|20)[0-9]{2}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])", ErrorMessage = "Nieprawidłowy format daty.")]
        public string DateOfBirth { get; set; }

        [Required(ErrorMessage = "Adres e-mail jest wymagany")]
        [EmailAddress(ErrorMessage = "Niepoprawny format adresu e-mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Wykształcenie jest wymagane")]
        public EducationLevel Education { get; set; }

        [AllowedFileExtensions("jpg, pdf, doc", ErrorMessage = "Niedozwolony format pliku")]
        public HttpPostedFileBase Attachment1 { get; set; }

        [AllowedFileExtensions("jpg, pdf, doc", ErrorMessage = "Niedozwolony format pliku")]
        public HttpPostedFileBase Attachment2 { get; set; }

        [AllowedFileExtensions("jpg, pdf, doc", ErrorMessage = "Niedozwolony format pliku")]
        public List<HttpPostedFileBase> AdditionalAttachments { get; set; }

        // Staże
        public List<Internship> Internships { get; set; }
    }

    public class AllowedFileExtensionsAttribute : ValidationAttribute
    {
        private readonly string _allowedExtensions;

        public AllowedFileExtensionsAttribute(string allowedExtensions)
        {
            _allowedExtensions = allowedExtensions;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is HttpPostedFileBase file)
            {
                var fileExtension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                var allowedExtensions = _allowedExtensions.Split(',').Select(x => x.Trim().ToLowerInvariant());

                if (!allowedExtensions.Contains(fileExtension))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }

    public enum EducationLevel
    {
        Podstawowe,
        Średnie,
        Wyższe
    }

    public class Internship
    {

        [Required(ErrorMessage = "Nazwa firmy stażowej jest wymagana")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Data rozpoczęcia stażu jest wymagana")]
        [RegularExpression(@"(19|20)[0-9]{2}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])", ErrorMessage = "Nieprawidłowy format daty.")]
        public string DateStart { get; set; }

        [RegularExpression(@"(19|20)[0-9]{2}-(0[1-9]|1[012])-(0[1-9]|[12][0-9]|3[01])", ErrorMessage = "Nieprawidłowy format daty.")]
        public string DateEnd { get; set; }
    }

    public class ApplicantController : Controller
    {
        // GET: Applicant
        public ActionResult Index()
        {
            return View();
        }

        // POST: Applicant
        [HttpPost]
        public ActionResult Index(ApplicantForm model, string connectionString)
        {
            if (ModelState.IsValid)
            {
                // jeśli po stronie bazy jest procedura to wtedy coś mniej więcej takiego
                /*using (SqlConnection conn = new SqlConnection(connectionString))
                {               
                     var textProcedure = "[].[]";
                    using (SqlCommand cmd = new SqlCommand(textProcedure, conn))
                    {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add(new SqlParameter("", ));
                            conn.Open();
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        using (var reader = cmd.ExecuteReader())    
                     }
                    */

                // trochę inny sposób

                using (var dbContext = new YourDb()) // Zastąpić trzeba YourDb odpowiednim kontekstem bazy danych
                    {

                        var applicant = new Applicant
                        {
                            FirstName = model.FirstName,
                            LastName = model.LastName,
                            DateOfBirth = model.DateOfBirth,
                            Email = model.Email,
                            Education = model.Education,
                        };

                        if (model.Attachment1 != null)
                        {
                            var attachment1Path = SaveAttachment(model.Attachment1);
                            applicant.Attachment1Path = attachment1Path;
                        }

                        if (model.Attachment2 != null)
                        {
                            var attachment2Path = SaveAttachment(model.Attachment2);
                            applicant.Attachment2Path = attachment2Path;
                        }

                        if (model.AdditionalAttachments != null && model.AdditionalAttachments.Count > 0)
                        {
                            applicant.AdditionalAttachmentPaths = new List<string>();

                            foreach (var attachment in model.AdditionalAttachments)
                            {
                                var attachmentPath = SaveAttachment(attachment);
                                applicant.AdditionalAttachmentPaths.Add(attachmentPath);
                            }
                        }

                        if (model.Internships != null && model.Internships.Count > 0)
                        {
                            applicant.Internships = new List<Internship>();

                            foreach (var internship in model.Internships)
                            {
                                applicant.Internships.Add(new Internship
                                {
                                    CompanyName = internship.CompanyName,
                                    DateStart = internship.DateStart,
                                    DateEnd = internship.DateEnd
                                });
                            }
                        }

                        dbContext.Applicants.Add(applicant);
                        dbContext.SaveChanges();
                    }             
                    
                    return RedirectToAction("ThankYou");
                }

                return View(model);
            }

        private string SaveAttachment(HttpPostedFileBase attachment)
            {
                var attachmentFileName = Path.GetFileName(attachment.FileName);
                var attachmentPath = Path.Combine(Server.MapPath("Attachments/"), attachmentFileName);
                attachment.SaveAs(attachmentPath);
                return attachmentPath;
            }       

            public ActionResult ThankYou()
            {
                return View();
            }
        }
    }
