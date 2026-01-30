using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using StudyMateAI.Models;

namespace StudyMateAI.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DatabaseService()
        {
            try
            {
                // Database dosyasını Data klasörüne kaydet
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string dataDirectory = Path.Combine(appDirectory, "Data");
                
                // Data klasörü yoksa oluştur
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                _dbPath = Path.Combine(dataDirectory, "studymate.db");
                _connectionString = $"Data Source={_dbPath};Version=3;";
                
                InitializeDatabase();
            }
            catch (Exception ex)
            {
                throw new Exception($"Veritabanı başlatılamadı: {ex.Message}", ex);
            }
        }

        private void InitializeDatabase()
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // Courses tablosu
                string createCoursesTable = @"
                    CREATE TABLE IF NOT EXISTS Courses (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL,
                        Code TEXT,
                        Credit INTEGER,
                        MidtermGrade REAL,
                        MidtermPercentage INTEGER DEFAULT 40,
                        FinalPercentage INTEGER DEFAULT 60,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    )";

                // Documents tablosu
                string createDocumentsTable = @"
                    CREATE TABLE IF NOT EXISTS Documents (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER,
                        FileName TEXT NOT NULL,
                        FilePath TEXT NOT NULL,
                        Content TEXT,
                        Analysis TEXT,
                        Summary TEXT,
                        Keywords TEXT,
                        MindMapData TEXT,
                        UserNotes TEXT,
                        UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
                    )";

                // ChatHistory tablosu
                string createChatHistoryTable = @"
                    CREATE TABLE IF NOT EXISTS ChatHistory (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER,
                        UserMessage TEXT,
                        AIResponse TEXT,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE SET NULL
                    )";

                // StudySessions tablosu
                string createStudySessionsTable = @"
                    CREATE TABLE IF NOT EXISTS StudySessions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        CourseId INTEGER,
                        Duration INTEGER,
                        Topic TEXT,
                        SessionDate DATETIME DEFAULT CURRENT_TIMESTAMP,
                        FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
                    )";

                // Folders table
                string createFoldersTable = @"
                    CREATE TABLE IF NOT EXISTS Folders (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL
                    )";

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = createFoldersTable;
                    command.ExecuteNonQuery();

                    // StudyPlans table
                    string createStudyPlansTable = @"
                        CREATE TABLE IF NOT EXISTS StudyPlans (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Subject TEXT NOT NULL,
                            GoalDescription TEXT,
                            TotalTargetHours REAL,
                            StartDate DATETIME,
                            EndDate DATETIME,
                            IsActive INTEGER DEFAULT 1
                        )";
                    command.CommandText = createStudyPlansTable;
                    command.ExecuteNonQuery();

                    // StudyTasks table
                    string createStudyTasksTable = @"
                        CREATE TABLE IF NOT EXISTS StudyTasks (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            StudyPlanId INTEGER,
                            Topic TEXT,
                            EstimatedHours REAL,
                            CompletedHours REAL,
                            Status TEXT,
                            FOREIGN KEY (StudyPlanId) REFERENCES StudyPlans(Id) ON DELETE CASCADE
                        )";
                    command.CommandText = createStudyTasksTable;
                    command.ExecuteNonQuery();

                    // DailySchedules table
                    string createDailySchedulesTable = @"
                        CREATE TABLE IF NOT EXISTS DailySchedules (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            Date TEXT NOT NULL,
                            StudyPlanId INTEGER,
                            PlannedMinutes INTEGER,
                            IsCompleted INTEGER DEFAULT 0,
                            TaskTopic TEXT,
                            FOREIGN KEY (StudyPlanId) REFERENCES StudyPlans(Id) ON DELETE CASCADE
                        )";
                    command.CommandText = createDailySchedulesTable;
                    command.ExecuteNonQuery();

                    // Migration: Add TaskTopic column if it doesn't exist
                    try
                    {
                        string addTaskTopicColumn = "ALTER TABLE DailySchedules ADD COLUMN TaskTopic TEXT";
                        command.CommandText = addTaskTopicColumn;
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        // Column already exists, ignore error
                    }

                    // VideoResources table
                    string createVideoResourcesTable = @"
                        CREATE TABLE IF NOT EXISTS VideoResources (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            CourseId INTEGER,
                            Title TEXT,
                            YouTubeUrl TEXT,
                            VideoId TEXT,
                            Duration INTEGER,
                            Notes TEXT,
                            Transcript TEXT,
                            DateAdded TEXT,
                            FOREIGN KEY (CourseId) REFERENCES Courses(Id) ON DELETE CASCADE
                        )";
                    command.CommandText = createVideoResourcesTable;
                    command.ExecuteNonQuery();

                    // VideoNotes table
                    string createVideoNotesTable = @"
                        CREATE TABLE IF NOT EXISTS VideoNotes (
                            Id INTEGER PRIMARY KEY AUTOINCREMENT,
                            VideoResourceId INTEGER,
                            Timestamp INTEGER,
                            Note TEXT,
                            CreatedDate TEXT,
                            FOREIGN KEY (VideoResourceId) REFERENCES VideoResources(Id) ON DELETE CASCADE
                        )";
                    command.CommandText = createVideoNotesTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createCoursesTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createDocumentsTable;
                    command.ExecuteNonQuery();

                    // Check if Analysis column exists in Documents table (Migration)
                    // ... (existing Analysis check) ...

                    // Migration for Document Enhancements
                    try
                    {
                        command.CommandText = "ALTER TABLE Documents ADD COLUMN Summary TEXT";
                        command.ExecuteNonQuery();
                        command.CommandText = "ALTER TABLE Documents ADD COLUMN Keywords TEXT";
                        command.ExecuteNonQuery();
                        command.CommandText = "ALTER TABLE Documents ADD COLUMN MindMapData TEXT";
                        command.ExecuteNonQuery();
                    }
                    catch { }

                    // Migration for UserNotes
                    try
                    {
                        command.CommandText = "ALTER TABLE Documents ADD COLUMN UserNotes TEXT";
                        command.ExecuteNonQuery();
                    }
                    catch { }

                    // Check if FolderId column exists in Documents table (Folder Migration)
                    // Check if FolderId column exists using PRAGMA
                    command.CommandText = "PRAGMA table_info(Documents);";
                    bool hasFolderId = false;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["name"].ToString() == "FolderId")
                            {
                                hasFolderId = true;
                                break;
                            }
                        }
                    }

                    if (!hasFolderId)
                    {
                        // Use a new command for ALTER explicitly to avoid reader conflict state issues
                        using (var alterCmd = new SQLiteCommand("ALTER TABLE Documents ADD COLUMN FolderId INTEGER REFERENCES Folders(Id) ON DELETE SET NULL", connection))
                        {
                            alterCmd.ExecuteNonQuery();
                        }
                    }
                
                // ... (rest of tables)

                    command.CommandText = "PRAGMA table_info(Documents);";
                    bool hasAnalysisColumn = false;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["name"].ToString() == "Analysis")
                            {
                                hasAnalysisColumn = true;
                                break;
                            }
                        }
                    }

                    if (!hasAnalysisColumn)
                    {
                        using (var alterCommand = new SQLiteCommand("ALTER TABLE Documents ADD COLUMN Analysis TEXT;", connection))
                        {
                            alterCommand.ExecuteNonQuery();
                        }
                    }

                    command.CommandText = createChatHistoryTable;
                    command.ExecuteNonQuery();

                    command.CommandText = createStudySessionsTable;
                    command.ExecuteNonQuery();

                    // Migration for VideoResources Transcript column
                    try
                    {
                        command.CommandText = "ALTER TABLE VideoResources ADD COLUMN Transcript TEXT";
                        command.ExecuteNonQuery();
                    }
                    catch
                    {
                        // Column likely exists
                    }
                }
            }
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(_connectionString);
        }

        public string GetDatabasePath()
        {
            return _dbPath;
        }

        public System.Collections.Generic.List<Models.Course> GetAllCourses()
        {
            var courses = new System.Collections.Generic.List<Models.Course>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM Courses ORDER BY Name";
                using (var command = new SQLiteCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        courses.Add(new Models.Course
                        {
                            Id = reader.GetInt32(0),
                            Name = reader.GetString(1),
                            Code = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                            Credit = reader.GetInt32(3),
                            MidtermGrade = reader.IsDBNull(4) ? null : reader.GetDouble(4),
                            MidtermPercentage = reader.GetInt32(5),
                            FinalPercentage = reader.GetInt32(6)
                        });
                    }
                }
            }
            return courses;
        }

        // Chat Message Operations
        public void SaveChatMessage(int? courseId, string userMessage, string aiResponse)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"INSERT INTO ChatHistory (CourseId, UserMessage, AIResponse) 
                                VALUES (@courseId, @userMessage, @aiResponse)";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@courseId", courseId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@userMessage", userMessage);
                    command.Parameters.AddWithValue("@aiResponse", aiResponse);
                    command.ExecuteNonQuery();
                }
            }
        }

        public System.Collections.Generic.List<Models.ChatMessage> GetChatHistory(int? courseId = null)
        {
            var messages = new System.Collections.Generic.List<Models.ChatMessage>();
            
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = courseId.HasValue 
                    ? "SELECT * FROM ChatHistory WHERE CourseId = @courseId ORDER BY Timestamp ASC"
                    : "SELECT * FROM ChatHistory ORDER BY Timestamp ASC";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    if (courseId.HasValue)
                        command.Parameters.AddWithValue("@courseId", courseId.Value);
                    
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            messages.Add(new Models.ChatMessage
                            {
                                Id = reader.GetInt32(0),
                                CourseId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1),
                                UserMessage = reader.GetString(2),
                                AIResponse = reader.GetString(3),
                                Timestamp = reader.GetDateTime(4)
                            });
                        }
                    }
                }
            }
            
            return messages;
        }

        public void ClearChatHistory()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM ChatHistory";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        // Document Operations


        public void DeleteDocument(int documentId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM Documents WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }
        public void UpdateDocumentAnalysis(int documentId, string analysis)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET Analysis = @analysis WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@analysis", analysis ?? string.Empty);
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateDocumentCourse(int documentId, int? courseId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET CourseId = @courseId WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@courseId", (object?)courseId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateDocumentContent(int documentId, string content)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET Content = @content WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@content", content ?? string.Empty);
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateDocumentNotes(int documentId, string notes)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET UserNotes = @notes WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@notes", notes ?? string.Empty);
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void ClearAllDocuments()
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM Documents";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
        public void AddFolder(Folder folder)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "INSERT INTO Folders (Name) VALUES (@name)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@name", folder.Name);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Folder> GetAllFolders()
        {
            var folders = new List<Folder>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Folders ORDER BY Name";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            folders.Add(new Folder
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return folders;
        }

        public void DeleteFolder(int folderId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM Folders WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", folderId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        // Updated AddDocument to include FolderId
        public void UpdateDocumentFolder(int documentId, int? folderId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET FolderId = @folderId WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@folderId", (object?)folderId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@id", documentId);
                    command.ExecuteNonQuery();
                }
            }
        }
        
        // --- Study Planner Methods ---

        public int AddStudyPlan(StudyPlan plan)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"INSERT INTO StudyPlans (Subject, GoalDescription, TotalTargetHours, StartDate, EndDate, IsActive) 
                                VALUES (@subject, @goal, @target, @start, @end, @active);
                                SELECT last_insert_rowid()";
                using (var command = new SQLiteCommand(query, connection))
                {
                     command.Parameters.AddWithValue("@subject", plan.Subject);
                     command.Parameters.AddWithValue("@goal", plan.GoalDescription);
                     command.Parameters.AddWithValue("@target", plan.TotalTargetHours);
                     command.Parameters.AddWithValue("@start", plan.StartDate);
                     command.Parameters.AddWithValue("@end", plan.EndDate);
                     command.Parameters.AddWithValue("@active", plan.IsActive ? 1 : 0);
                     
                     return Convert.ToInt32(command.ExecuteScalar());
                }
            }
        }

        public List<StudyPlan> GetActiveStudyPlans()
        {
            var plans = new List<StudyPlan>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM StudyPlans WHERE IsActive = 1 ORDER BY StartDate DESC";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            plans.Add(new StudyPlan
                            {
                                Id = reader.GetInt32(0),
                                Subject = reader.GetString(1),
                                GoalDescription = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TotalTargetHours = reader.GetDouble(3),
                                StartDate = reader.GetDateTime(4),
                                EndDate = reader.GetDateTime(5),
                                IsActive = reader.GetInt32(6) == 1
                            });
                        }
                    }
                }
            }
            return plans;
        }

        public void ArchiveStudyPlan(int planId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE StudyPlans SET IsActive = 0 WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", planId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteStudyPlan(int planId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                // CASCADE will delete associated tasks and schedules
                string query = "DELETE FROM StudyPlans WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", planId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public StudyPlan? GetStudyPlanById(int planId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM StudyPlans WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", planId);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new StudyPlan
                            {
                                Id = reader.GetInt32(0),
                                Subject = reader.GetString(1),
                                GoalDescription = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                TotalTargetHours = reader.GetDouble(3),
                                StartDate = reader.GetDateTime(4),
                                EndDate = reader.GetDateTime(5),
                                IsActive = reader.GetInt32(6) == 1
                            };
                        }
                    }
                }
            }
            return null;
        }

        public void AddStudyTask(StudyTask task)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"INSERT INTO StudyTasks (StudyPlanId, Topic, EstimatedHours, CompletedHours, Status) 
                                VALUES (@planId, @topic, @est, @comp, @status)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@planId", task.StudyPlanId);
                    command.Parameters.AddWithValue("@topic", task.Topic);
                    command.Parameters.AddWithValue("@est", task.EstimatedHours);
                    command.Parameters.AddWithValue("@comp", task.CompletedHours);
                    command.Parameters.AddWithValue("@status", task.Status);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<StudyTask> GetTasksForPlan(int planId)
        {
            var tasks = new List<StudyTask>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT * FROM StudyTasks WHERE StudyPlanId = @planId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@planId", planId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            tasks.Add(new StudyTask
                            {
                                Id = reader.GetInt32(0),
                                StudyPlanId = reader.GetInt32(1),
                                Topic = reader.GetString(2),
                                EstimatedHours = reader.GetDouble(3),
                                CompletedHours = reader.GetDouble(4),
                                Status = reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return tasks;
        }

        public void UpdateTaskProgress(int taskId, double hoursToAdd)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE StudyTasks SET CompletedHours = CompletedHours + @hours WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@hours", hoursToAdd);
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void UpdateTaskStatus(int taskId, string status)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE StudyTasks SET Status = @status WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@status", status);
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteStudyTask(int taskId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM StudyTasks WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", taskId);
                    command.ExecuteNonQuery();
                }
            }
        }

        // --- Daily Schedule Methods ---

        public void AddDailySchedule(DailySchedule schedule)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"INSERT INTO DailySchedules (Date, StudyPlanId, PlannedMinutes, IsCompleted, TaskTopic) 
                                VALUES (@date, @planId, @minutes, @completed, @taskTopic)";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@date", schedule.Date.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@planId", schedule.StudyPlanId);
                    command.Parameters.AddWithValue("@minutes", schedule.PlannedMinutes);
                    command.Parameters.AddWithValue("@completed", schedule.IsCompleted ? 1 : 0);
                    command.Parameters.AddWithValue("@taskTopic", schedule.TaskTopic ?? "");
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<DailySchedule> GetScheduleForWeek(DateTime startDate)
        {
            var schedules = new List<DailySchedule>();
            using (var connection = GetConnection())
            {
                connection.Open();
                var endDate = startDate.AddDays(7);
                string query = "SELECT * FROM DailySchedules WHERE Date >= @start AND Date < @end ORDER BY Date";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@start", startDate.ToString("yyyy-MM-dd"));
                    command.Parameters.AddWithValue("@end", endDate.ToString("yyyy-MM-dd"));
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            schedules.Add(new DailySchedule
                            {
                                Id = reader.GetInt32(0),
                                Date = DateTime.Parse(reader.GetString(1)),
                                StudyPlanId = reader.GetInt32(2),
                                PlannedMinutes = reader.GetInt32(3),
                                IsCompleted = reader.GetInt32(4) == 1,
                                TaskTopic = reader.IsDBNull(5) ? "" : reader.GetString(5)
                            });
                        }
                    }
                }
            }
            return schedules;
        }

        public void UpdateScheduleCompletion(int scheduleId, bool isCompleted)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE DailySchedules SET IsCompleted = @completed WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@completed", isCompleted ? 1 : 0);
                    command.Parameters.AddWithValue("@id", scheduleId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteDailySchedule(int scheduleId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM DailySchedules WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", scheduleId);
                    command.ExecuteNonQuery();
                }
            }
        }


        public void AddDocument(Document document)
        {
            using (var connection = GetConnection())
            {
                 connection.Open();
                 string query = @"INSERT INTO Documents (CourseId, FolderId, FileName, FilePath, Content, Analysis, Summary, Keywords, UserNotes, UploadedAt) 
                                 VALUES (@courseId, @folderId, @fileName, @filePath, @content, @analysis, @summary, @keywords, @userNotes, @uploadedAt)";
                 
                 using (var command = new SQLiteCommand(query, connection))
                 {
                     command.Parameters.AddWithValue("@courseId", (object?)document.CourseId ?? DBNull.Value);
                     command.Parameters.AddWithValue("@folderId", (object?)document.FolderId ?? DBNull.Value);
                     command.Parameters.AddWithValue("@fileName", document.FileName);
                     command.Parameters.AddWithValue("@filePath", document.FilePath);
                     command.Parameters.AddWithValue("@content", document.Content ?? string.Empty);
                     command.Parameters.AddWithValue("@analysis", document.Analysis ?? string.Empty);
                     command.Parameters.AddWithValue("@summary", document.Summary ?? string.Empty);
                     command.Parameters.AddWithValue("@keywords", document.Keywords ?? string.Empty);
                     command.Parameters.AddWithValue("@userNotes", document.UserNotes ?? string.Empty);
                     command.Parameters.AddWithValue("@uploadedAt", document.UploadedAt);
                     command.ExecuteNonQuery();
                 }
            }
        }

        public List<Document> GetAllDocuments()
        {
            var documents = new List<Document>();
            using (var connection = GetConnection())
            {
                connection.Open();
                // Explicitly select columns to avoid index issues with Alter Table
                string query = "SELECT Id, CourseId, FolderId, FileName, FilePath, Content, Analysis, UploadedAt, Summary, Keywords, UserNotes FROM Documents ORDER BY UploadedAt DESC";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            documents.Add(new Document
                            {
                                Id = reader.GetInt32(0),
                                CourseId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                                FolderId = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                                FileName = reader.GetString(3),
                                FilePath = reader.GetString(4),
                                Content = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Analysis = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                UploadedAt = reader.GetDateTime(7),
                                Summary = !reader.IsDBNull(8) ? reader.GetString(8) : string.Empty,
                                Keywords = !reader.IsDBNull(9) ? reader.GetString(9) : string.Empty,
                                UserNotes = !reader.IsDBNull(10) ? reader.GetString(10) : string.Empty
                            });
                        }
                    }
                }
            }
            return documents;
        }

        public void UpdateDocumentDetailedAnalysis(int id, string summary, string keywords)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE Documents SET Summary = @summary, Keywords = @keywords WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@summary", summary ?? string.Empty);
                    command.Parameters.AddWithValue("@keywords", keywords ?? string.Empty);
                    command.ExecuteNonQuery();
                }
            }
        }

        // --- Video Resource Methods ---

        public void SaveVideoResource(VideoResource video)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = @"INSERT INTO VideoResources (CourseId, Title, YouTubeUrl, VideoId, Duration, Notes, Transcript, DateAdded) 
                                VALUES (@courseId, @title, @url, @videoId, @duration, @notes, @transcript, @dateAdded)";
                
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@courseId", (object?)video.CourseId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@title", video.Title ?? string.Empty);
                    command.Parameters.AddWithValue("@url", video.YouTubeUrl ?? string.Empty);
                    command.Parameters.AddWithValue("@videoId", video.VideoId ?? string.Empty);
                    command.Parameters.AddWithValue("@duration", video.Duration);
                    command.Parameters.AddWithValue("@notes", video.Notes ?? string.Empty);
                    command.Parameters.AddWithValue("@transcript", video.Transcript ?? string.Empty);
                    command.Parameters.AddWithValue("@dateAdded", video.DateAdded.ToString("yyyy-MM-dd HH:mm:ss"));
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<VideoResource> GetSavedVideos()
        {
            var videos = new List<VideoResource>();
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT Id, CourseId, Title, YouTubeUrl, VideoId, Duration, Notes, DateAdded, Transcript FROM VideoResources ORDER BY DateAdded DESC";
                using (var command = new SQLiteCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var video = new VideoResource
                            {
                                Id = reader.GetInt32(0),
                                CourseId = reader.IsDBNull(1) ? null : (int?)reader.GetInt32(1),
                                Title = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                                YouTubeUrl = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                                VideoId = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                                Duration = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                                Notes = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                                DateAdded = DateTime.Parse(reader.GetString(7)),
                                Transcript = reader.IsDBNull(8) ? string.Empty : reader.GetString(8)
                            };
                            videos.Add(video);
                        }
                    }
                }
            }
            return videos;
        }

        public void DeleteVideoResource(int videoId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "DELETE FROM VideoResources WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", videoId);
                    command.ExecuteNonQuery();
                }
            }
        }

        public bool IsVideoSaved(string videoId)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "SELECT COUNT(*) FROM VideoResources WHERE VideoId = @videoId";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@videoId", videoId);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public void UpdateVideoTitle(int videoId, string newTitle)
        {
            using (var connection = GetConnection())
            {
                connection.Open();
                string query = "UPDATE VideoResources SET Title = @title WHERE Id = @id";
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@title", newTitle);
                    command.Parameters.AddWithValue("@id", videoId);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
