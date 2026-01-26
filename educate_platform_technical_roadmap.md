# Educate Platform – Technical Roadmap

## Overview
This technical roadmap outlines the phased development plan for the Educate professional examination platform. It covers MVP delivery through full product maturity, with clear milestones, timelines, and technical outcomes. The roadmap assumes a single backend-focused developer, a security-first approach, and a modular .NET + PostgreSQL architecture.

---

## Timeline Summary

- **MVP Delivery:** 12–14 weeks
- **Full Product Build:** Additional 12–16 weeks
- **Total Duration:** ~6–7 months

---

# MVP ROADMAP

## Milestone 1 – Project Foundation & Architecture
**Timeline:** Week 1

### Objectives
Establish a scalable, secure, and maintainable backend foundation.

### Deliverables
- Multi-project solution structure:
  - Educate.API
  - Educate.Application
  - Educate.Domain
  - Educate.Infrastructure
- PostgreSQL integration
- Entity Framework Core setup with migrations
- Environment separation (Development, Staging, Production)
- Dotnet User-Secrets configuration
- Global exception handling
- Centralized logging strategy

### Outcome
A clean architectural base ready for feature development without rework.

---

## Milestone 2 – Authentication & Identity Management
**Timeline:** Weeks 2–3

### Objectives
Implement a secure and extensible authentication system.

### Deliverables
- ASP.NET Core Identity integration
- Extended user profile data:
  - First Name
  - Last Name
  - Username
  - Email
  - Phone Number
- Secure password hashing
- JWT-based authentication and authorization
- Refresh token strategy
- Email confirmation using SendGrid (REST API)
- Login notification emails
- Google OAuth registration
- Mandatory password setup for Google-registered users
- Forgot password flow (email-based)
- Account state tracking (verified, active, last login)

### Outcome
A production-grade identity system with strong security and flexibility.

---

## Milestone 3 – Course, Level & Subject Management (Admin)
**Timeline:** Weeks 4–5

### Objectives
Model professional examinations accurately and flexibly.

### Deliverables
- Course management (Courses can be added by the Admin)
- Level management (Levels can be added by the Admin)
- Subject management per level (Subjects can be added by the Admin)
- Admin-only CRUD operations
- Soft-delete strategy

### Outcome
A robust academic structure capable of supporting multiple professional exams.

---

## Milestone 4 – User Enrollment & Subscription Logic
**Timeline:** Weeks 6–7

### Objectives
Enable users to enroll in courses and levels with correct entitlement rules.

### Deliverables
- User-course-level enrollment model
- Subscription logic:
  - Per level
  - Bi-annual (6 months)
- Subscription states:
  - Active
  - Pending
  - Expired
  - Cancelled
- Support for multiple concurrent subscriptions
- Access control enforcement based on subscription state

### Outcome
Clear and enforceable access rules before payment integration.

---

## Milestone 5 – Payments Integration (Paystack)
**Timeline:** Weeks 8–9

### Objectives
Implement secure, verifiable payment processing.

### Deliverables
- Unified payment abstraction layer
- Paystack integration:
  - Transaction initialization
  - Server-side verification
  - Secure webhook handling
  - Webhook signature validation
  - Server-side payment verification only
  - Persistent payment records
  - Receipt generation and storage
  - Subscription activation on successful payment

### Outcome
A reliable financial system with audit-ready records.

---

## Milestone 6 – Subscription Renewal & Expiry Handling
**Timeline:** Week 10

### Objectives
Automate subscription lifecycle management.

### Deliverables
- Expiry tracking
- Background jobs for subscription monitoring
- Renewal reminders:
  - 30 days before expiry
  - 7 days before expiry
  - On expiry
- Email notifications
- In-app notifications
- Grace-period logic

### Outcome
Reduced churn and minimal manual intervention.

---

## Milestone 7 – User Dashboard (MVP Scope)
**Timeline:** Weeks 11–12

### Objectives
Provide users with visibility and control.

### Deliverables
- Dashboard APIs
- Enrolled courses and levels
- Subscription status and expiry dates
- Subject listings per level
- Basic practice material access
- Payment history

### Outcome
A functional MVP that delivers real user value.

---

# FULL PRODUCT ROADMAP

## Milestone 8 – Practice Tests & Mock Exams System
**Timeline:** Weeks 13–15

### Deliverables
- Question bank system
- MCQ and multi-select support
- Timed exams
- Question randomization
- Attempt tracking
- Auto-grading
- Score storage

---

## Milestone 9 – Progress Tracking & Analytics
**Timeline:** Weeks 16–17

### Deliverables
- Subject-level performance metrics
- Level completion tracking
- Weak-area identification
- Exam history
- Exportable reports

---

## Milestone 10 – Advanced Admin Panel
**Timeline:** Weeks 18–20

### Deliverables
- Student management tools
- Subscription oversight
- Payment analytics
- Bulk uploads (CSV)
- Question moderation
- Course performance analytics
- Admin audit logs

---

## Milestone 11 – Security Hardening & Compliance
**Timeline:** Weeks 21–22

### Deliverables
- Rate limiting
- Account lockout rules
- Role-based access control
- Admin action auditing
- Webhook replay protection
- Security testing checklist

---

## Milestone 12 – Production Readiness
**Timeline:** Weeks 23–24

### Deliverables
- CI/CD pipelines
- Automated backups
- Monitoring and alerting
- Error tracking
- Deployment documentation
- Operational playbooks

---

## Final Notes
This roadmap prioritizes security, scalability, and long-term maintainability. It is designed to evolve from a focused MVP into a full professional examination platform without architectural rewrites.

