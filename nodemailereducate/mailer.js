import express from "express";
import nodemailer from "nodemailer";
import dotenv from "dotenv";
import cors from "cors";

dotenv.config();

const app = express();
app.use(express.json());
app.use(cors());

// Google SMTP Configuration
// Google SMTP Configuration
console.log("Configuring SMTP with User:", process.env.SMTP_USER ? "LOADED" : "MISSING");

const transporter = nodemailer.createTransport({
  host: 'smtp.gmail.com',
  port: 587,
  secure: false, // true for 465, false for other ports
  auth: {
    user: process.env.SMTP_USER,
    pass: process.env.SMTP_PASS,
  },
});

app.post("/api/send-email", async (req, res) => {
  try {
    const { to, subject, html } = req.body;

    // Message object
    let message = {
      from: process.env.FROM_EMAIL || process.env.SMTP_USER,
      to: to,
      subject: subject,
      html: html
    };

    let info = await transporter.sendMail(message);

    console.log('Message sent: %s', info.messageId);

    res.status(200).json({ success: true, message: "Email sent" });
  } catch (err) {
    console.error(err);
    res.status(500).json({ success: false, message: "Email sending failed" });
  }
});

app.listen(4000, () => {
  console.log("Email service running on port 4000");
});
