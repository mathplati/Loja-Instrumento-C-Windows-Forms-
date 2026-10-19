# Sonora · Loja de Instrumentos

Sistema acadêmico de vendas em C# / .NET 8. A entrega desktop utiliza Windows Forms. Uma interface web de apoio permite explorar o mesmo catálogo, serviço de vendas e relatório no Linux.

## Estrutura

- `src/Sonora.Core`: catálogo, regras de venda e persistência JSON.
- `src/Sonora.Desktop`: Splash, Menu, Venda e Relatório em Windows Forms.
- `src/Sonora.Preview`: interface web de apoio, com o mesmo serviço C#.
- `tests/Sonora.Checks`: verificações executáveis sem pacotes externos.

O aplicativo ainda está em construção. Os próximos commits acrescentam cada parte separadamente.
