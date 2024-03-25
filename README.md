# Payment Consumer

Este projeto consiste num consumer da fila de pagamentos que é utilizado pelo projeto de API PIX disponível neste repositório: `https://github.com/pablodru/pix-stone`;

## Tecnologias 🔧

Para a construção do projeto foi utilizado as seguintes tecnologias:

- .Net: v8.0.202
- RabbitMQ

## Instalação e Execução 🚀

Para rodar o projeto localmente, siga os seguinter passos:

1. Clone o repositório: `git clone https://github.com/pablodru/pix-stone-payment-consumer.git`;
2. Acesse o diretório do projeto: `cd pix-stone-payment-consumer`;
3. Certifique-se de ter o RabbitMQ rodando em sua máquina e as credenciais de conexão com ele; 
4. Rode o projeto com: `dotnet run`;
5. Depois disso, a aplicação estará consumindo a fila de Payments;

Nota: O consumer faz requisições a uma PSP, então certifique-se de ter um mock rodando na porta utilizada.
