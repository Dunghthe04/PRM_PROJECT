IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [FeeCategories] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [DefaultAmount] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_FeeCategories] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Semesters] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [StartDate] datetime2 NOT NULL,
    [EndDate] datetime2 NOT NULL,
    CONSTRAINT [PK_Semesters] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Subjects] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Code] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Subjects] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(max) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [FullName] nvarchar(max) NOT NULL,
    [AvatarUrl] nvarchar(max) NOT NULL,
    [Role] int NOT NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Classes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [SemesterId] int NOT NULL,
    CONSTRAINT [PK_Classes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Classes_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [FeeInvoices] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [FeeCategoryId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [IsPaid] bit NOT NULL,
    [PaidAt] datetime2 NULL,
    [PaymentMethod] nvarchar(max) NULL,
    [TransactionId] nvarchar(max) NULL,
    CONSTRAINT [PK_FeeInvoices] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FeeInvoices_FeeCategories_FeeCategoryId] FOREIGN KEY ([FeeCategoryId]) REFERENCES [FeeCategories] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_FeeInvoices_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Grades] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [SemesterId] int NOT NULL,
    [Score] float NOT NULL,
    [AssessmentType] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Grades] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Grades_Semesters_SemesterId] FOREIGN KEY ([SemesterId]) REFERENCES [Semesters] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Grades_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Grades_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [LeaveRequests] (
    [Id] int NOT NULL IDENTITY,
    [StudentId] int NOT NULL,
    [Date] datetime2 NOT NULL,
    [Reason] nvarchar(max) NOT NULL,
    [MedicalCertificateUrl] nvarchar(max) NULL,
    [Status] int NOT NULL,
    [ApprovedByTeacherId] int NULL,
    CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LeaveRequests_Users_ApprovedByTeacherId] FOREIGN KEY ([ApprovedByTeacherId]) REFERENCES [Users] ([Id]),
    CONSTRAINT [FK_LeaveRequests_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Notifications] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(max) NOT NULL,
    [Message] nvarchar(max) NOT NULL,
    [IsRead] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [StudentParents] (
    [ParentId] int NOT NULL,
    [StudentId] int NOT NULL,
    CONSTRAINT [PK_StudentParents] PRIMARY KEY ([StudentId], [ParentId]),
    CONSTRAINT [FK_StudentParents_Users_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_StudentParents_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Announcements] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [Type] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [CreatedById] int NOT NULL,
    [TargetClassId] int NULL,
    CONSTRAINT [PK_Announcements] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Announcements_Classes_TargetClassId] FOREIGN KEY ([TargetClassId]) REFERENCES [Classes] ([Id]),
    CONSTRAINT [FK_Announcements_Users_CreatedById] FOREIGN KEY ([CreatedById]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Assignments] (
    [Id] int NOT NULL IDENTITY,
    [Title] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [DueDate] datetime2 NOT NULL,
    [ClassId] int NOT NULL,
    [SubjectId] int NOT NULL,
    [CreatedByTeacherId] int NOT NULL,
    CONSTRAINT [PK_Assignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Assignments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Assignments_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Assignments_Users_CreatedByTeacherId] FOREIGN KEY ([CreatedByTeacherId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Attendances] (
    [Id] int NOT NULL IDENTITY,
    [ClassId] int NOT NULL,
    [StudentId] int NOT NULL,
    [Date] datetime2 NOT NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Attendances_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Attendances_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ClassStudents] (
    [ClassId] int NOT NULL,
    [StudentId] int NOT NULL,
    CONSTRAINT [PK_ClassStudents] PRIMARY KEY ([ClassId], [StudentId]),
    CONSTRAINT [FK_ClassStudents_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ClassStudents_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [TeacherAssignments] (
    [Id] int NOT NULL IDENTITY,
    [TeacherId] int NOT NULL,
    [ClassId] int NOT NULL,
    [SubjectId] int NOT NULL,
    CONSTRAINT [PK_TeacherAssignments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_TeacherAssignments_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TeacherAssignments_Subjects_SubjectId] FOREIGN KEY ([SubjectId]) REFERENCES [Subjects] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_TeacherAssignments_Users_TeacherId] FOREIGN KEY ([TeacherId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Submissions] (
    [Id] int NOT NULL IDENTITY,
    [AssignmentId] int NOT NULL,
    [StudentId] int NOT NULL,
    [FileUrl] nvarchar(max) NOT NULL,
    [SubmittedAt] datetime2 NOT NULL,
    [Score] float NULL,
    [Feedback] nvarchar(max) NULL,
    CONSTRAINT [PK_Submissions] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Submissions_Assignments_AssignmentId] FOREIGN KEY ([AssignmentId]) REFERENCES [Assignments] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Submissions_Users_StudentId] FOREIGN KEY ([StudentId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Announcements_CreatedById] ON [Announcements] ([CreatedById]);
GO

CREATE INDEX [IX_Announcements_TargetClassId] ON [Announcements] ([TargetClassId]);
GO

CREATE INDEX [IX_Assignments_ClassId] ON [Assignments] ([ClassId]);
GO

CREATE INDEX [IX_Assignments_CreatedByTeacherId] ON [Assignments] ([CreatedByTeacherId]);
GO

CREATE INDEX [IX_Assignments_SubjectId] ON [Assignments] ([SubjectId]);
GO

CREATE INDEX [IX_Attendances_ClassId] ON [Attendances] ([ClassId]);
GO

CREATE INDEX [IX_Attendances_StudentId] ON [Attendances] ([StudentId]);
GO

CREATE INDEX [IX_Classes_SemesterId] ON [Classes] ([SemesterId]);
GO

CREATE INDEX [IX_ClassStudents_StudentId] ON [ClassStudents] ([StudentId]);
GO

CREATE INDEX [IX_FeeInvoices_FeeCategoryId] ON [FeeInvoices] ([FeeCategoryId]);
GO

CREATE INDEX [IX_FeeInvoices_StudentId] ON [FeeInvoices] ([StudentId]);
GO

CREATE INDEX [IX_Grades_SemesterId] ON [Grades] ([SemesterId]);
GO

CREATE INDEX [IX_Grades_StudentId] ON [Grades] ([StudentId]);
GO

CREATE INDEX [IX_Grades_SubjectId] ON [Grades] ([SubjectId]);
GO

CREATE INDEX [IX_LeaveRequests_ApprovedByTeacherId] ON [LeaveRequests] ([ApprovedByTeacherId]);
GO

CREATE INDEX [IX_LeaveRequests_StudentId] ON [LeaveRequests] ([StudentId]);
GO

CREATE INDEX [IX_Notifications_UserId] ON [Notifications] ([UserId]);
GO

CREATE INDEX [IX_StudentParents_ParentId] ON [StudentParents] ([ParentId]);
GO

CREATE INDEX [IX_Submissions_AssignmentId] ON [Submissions] ([AssignmentId]);
GO

CREATE INDEX [IX_Submissions_StudentId] ON [Submissions] ([StudentId]);
GO

CREATE INDEX [IX_TeacherAssignments_ClassId] ON [TeacherAssignments] ([ClassId]);
GO

CREATE INDEX [IX_TeacherAssignments_SubjectId] ON [TeacherAssignments] ([SubjectId]);
GO

CREATE INDEX [IX_TeacherAssignments_TeacherId] ON [TeacherAssignments] ([TeacherId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260612094646_InitialCreate', N'8.0.6');
GO

COMMIT;
GO

