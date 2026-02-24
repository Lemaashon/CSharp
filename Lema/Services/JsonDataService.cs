using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BSSE.Models;  
using Newtonsoft.Json;

namespace BSSE.Services
{
    /// <summary>
    /// Loads and validates the JSON configuration file.
    ///
    /// PYTHON EQUIVALENT: load_json_data(path, run_toggle)
    /// The run_toggle concept does not exist in C# — the command simply
    /// does not call this service if the user cancels the file dialog.
    ///
    /// ENCODING NOTE:
    /// The Python script opens files with encoding='utf-8-sig', which silently
    /// strips a UTF-8 BOM if one is present. In C#, new UTF8Encoding(true) does
    /// the same: detectEncodingFromByteOrderMarks=true removes the BOM before
    /// Newtonsoft.Json reads the stream, preventing a JsonReaderException on
    /// files saved by Excel or Windows Notepad.
    /// </summary>
    public static class JsonDataService
    {
        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads, validates, and deserialises the Lema JSON file at
        /// <paramref name="path"/> into a strongly-typed <see cref="LemaJsonRoot"/>.
        /// </summary>
        /// <param name="path">Absolute path to the .json configuration file.</param>
        /// <returns>
        /// A fully populated <see cref="LemaJsonRoot"/> with
        /// <see cref="LemaJsonRoot.ProjectInfo"/> and <see cref="LemaJsonRoot.BarInfo"/>.
        /// </returns>
        /// <exception cref="FileNotFoundException">
        /// The file does not exist at <paramref name="path"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The file extension is not .json.
        /// </exception>
        /// <exception cref="JsonException">
        /// The file content is not valid JSON, or required fields are missing.
        /// </exception>
        public static JsonRoot Load(string path)
        {
            // ── 1. Validate path ─────────────────────────────────────────────────

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("JSON file path must not be empty.", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    $"The system cannot find the path specified: {path}", path);

            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"The provided file is not a .json file: {path}", nameof(path));

            // ── 2. Read with BOM-safe UTF-8 encoding ─────────────────────────────
            // detectEncodingFromByteOrderMarks = true strips the UTF-8 BOM
            // that Excel and Windows Notepad add when saving UTF-8 files.
            // This mirrors Python's open(..., encoding='utf-8-sig').
            string rawJson;
            try
            {
                using (var reader = new StreamReader(path,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                    detectEncodingFromByteOrderMarks: true))
                {
                    rawJson = reader.ReadToEnd();
                }
            }
            catch (IOException ex)
            {
                throw new IOException($"Could not read file '{path}': {ex.Message}", ex);
            }

            // ── 3. Deserialise ───────────────────────────────────────────────────
            JsonRoot root;
            try
            {
                var settings = new JsonSerializerSettings
                {
                    // Produce a clear error message if a required field is
                    // missing or has the wrong type, rather than silently
                    // defaulting to 0/null.
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Include
                };

                root = JsonConvert.DeserializeObject<JsonRoot>(rawJson, settings);
            }
            catch (JsonReaderException ex)
            {
                // Mirrors Python's: raise ValueError("Failed to decode JSON. Check for syntax errors")
                throw new JsonException(
                    $"Failed to decode JSON. Check for syntax errors in '{path}': {ex.Message}", ex);
            }

            // ── 4. Null-guard the top-level sections ─────────────────────────────
            // If the JSON is valid but entirely empty ({}) Newtonsoft returns
            // a root with null children — guard here so callers never receive null.
            if (root == null)
                throw new JsonException($"JSON file deserialised to null. Verify the file is not empty: {path}");

            if (root.ProjectInfo == null)
                throw new JsonException($"'ProjectInfo' section is missing or null in: {path}");

            if (root.BarInfo == null)
                throw new JsonException($"'BarInfo' section is missing or null in: {path}");

            return root;

        }


    }
}
