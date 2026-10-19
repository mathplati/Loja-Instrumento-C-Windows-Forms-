# Atendimento ao enunciado

| Requisito | Implementação / situação |
| --- | --- |
| C# Windows Forms em .NET | `src/Sonora.Desktop`, `net8.0-windows`, `UseWindowsForms` |
| Splash | `SplashForm`, abertura e sinal de transição para o contexto principal |
| Menu | `MenuForm`, navegação lateral e indicadores reais do dia |
| Venda de instrumentos | `SaleForm`, catálogo, cliente, quantidade, pagamento, desconto e gravação |
| Relatório de vendas | `ReportForm`, listagem, filtros, seleção, detalhes e CSV |
| `.gitignore` .NET / Visual Studio | Arquivo na raiz, excluindo dados locais, binários e configurações privadas |
| Branches `main` e `dev` | Ambas publicadas; implementação completa em `dev` |
| Commits parcelados | Histórico descritivo, com a base, domínio, telas, testes e documentação em etapas |
| Repositório público | Confirmado no GitHub |
| Repositório em organização | Usuário optou por manter o repositório atual da conta pessoal `mathplati` |
| `integrantes.txt` | Criado com URL; nomes e RMs reais aguardam informação da equipe |
| Write para integrantes | Depende dos usuários reais e da configuração pelo responsável do repositório |
| Três commits por tarefa e integrante | A equipe deverá comprovar sua participação real; o histórico inicial não cria participação fictícia |
| PRs para `dev` e revisão do Tech Lead | Fluxo documentado, ainda sem PRs/revisões da equipe registrados |

O aplicativo Windows Forms foi compilado e verificado em uma máquina Windows pelo GitHub Actions. Consulte [evidências e limites dos testes](VALIDACAO.md). A versão web é um apoio para o ambiente Linux; ela não substitui nenhuma das quatro telas nativas.
