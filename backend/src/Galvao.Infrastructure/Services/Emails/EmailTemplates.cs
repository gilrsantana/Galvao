namespace Galvao.Infrastructure.Services.Emails;

public static class EmailTemplates
{
    public static string GetConfirmationEmail(string confirmationLink)
    {
        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Confirme seu E-mail</title>
    <style>
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            background-color: #f4f6f9;
            color: #333333;
            margin: 0;
            padding: 0;
            -webkit-font-smoothing: antialiased;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
        }}
        .header {{
            background-color: #0f172a;
            padding: 40px 20px;
            text-align: center;
        }}
        .header h1 {{
            color: #ffffff;
            margin: 0;
            font-size: 28px;
            font-weight: 300;
            letter-spacing: 1px;
        }}
        .content {{
            padding: 40px 30px;
            line-height: 1.6;
        }}
        .content h2 {{
            color: #0f172a;
            font-size: 20px;
            margin-top: 0;
            margin-bottom: 20px;
        }}
        .content p {{
            color: #555555;
            font-size: 16px;
            margin-bottom: 30px;
        }}
        .button-container {{
            text-align: center;
            margin: 40px 0;
        }}
        .btn {{
            background-color: #2563eb;
            color: #ffffff !important;
            text-decoration: none;
            padding: 14px 30px;
            border-radius: 6px;
            font-size: 16px;
            font-weight: 500;
            display: inline-block;
            box-shadow: 0 4px 6px rgba(37, 99, 235, 0.2);
            transition: background-color 0.2s;
        }}
        .btn:hover {{
            background-color: #1d4ed8;
        }}
        .footer {{
            background-color: #f8fafc;
            padding: 20px;
            text-align: center;
            font-size: 13px;
            color: #94a3b8;
            border-top: 1px solid #e2e8f0;
        }}
        .footer a {{
            color: #64748b;
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>GALVÃO</h1>
        </div>
        <div class=""content"">
            <h2>Bem-vindo à nossa plataforma!</h2>
            <p>Olá,</p>
            <p>Estamos muito felizes em ter você conosco. Para começar a aproveitar todos os recursos e garantir a segurança da sua conta, por favor confirme o seu endereço de e-mail clicando no botão abaixo:</p>
            <div class=""button-container"">
                <a href=""{confirmationLink}"" class=""btn"">Confirmar E-mail</a>
            </div>
            <p>Se o botão acima não funcionar, você também pode copiar e colar o link abaixo no seu navegador:</p>
            <p style=""word-break: break-all; font-size: 14px; color: #2563eb;""><a href=""{confirmationLink}"">{confirmationLink}</a></p>
            <p>Este link é válido por 24 horas.</p>
            <p style=""margin-bottom: 0;"">Se você não realizou este cadastro, pode desconsiderar esta mensagem.</p>
            <p style=""margin-top: 30px;"">Atenciosamente,<br><strong>Equipe Galvão</strong></p>
        </div>
        <div class=""footer"">
            <p>&copy; 2026 Galvão. Todos os direitos reservados.</p>
        </div>
    </div>
</body>
</html>";
    }
}
