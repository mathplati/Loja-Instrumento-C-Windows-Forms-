# Sonora · Loja de Instrumentos

Sistema acadêmico de vendas em C# / .NET 8. A entrega desktop utiliza Windows Forms. Uma interface web de apoio permite explorar o mesmo catálogo, serviço de vendas e relatório no Linux.

A entrega principal é o Windows Forms. O workflow [Windows Forms no GitHub Actions](https://github.com/mathplati/Loja-Instrumento-C-Windows-Forms-/actions) compila e verifica as janelas no Windows e disponibiliza o pacote e as capturas nativas nos artefatos da execução, quando as etapas correspondentes terminam com sucesso.

[Galeria da interface web de apoio](docs/GALERIA.md) (estas imagens não representam as janelas nativas).

## Estrutura

- `src/Sonora.Core`: catálogo, regras de venda e persistência JSON.
- `src/Sonora.Desktop`: Splash, Menu, Venda e Relatório em Windows Forms.
- `src/Sonora.Preview`: interface web de apoio, com o mesmo serviço C#.
- `tests/Sonora.Checks`: verificações executáveis sem pacotes externos.
- `tests/Sonora.Desktop.Checks`: verificações dos formulários e capturas reais no Windows.

## Funcionalidades

- Splash com abertura e transição para o menu.
- Menu com receita do dia, quantidade de vendas, instrumentos e ticket médio.
- Navegação lateral em uma janela única, preservando o pedido ao alternar entre páginas.
- Catálogo de oito instrumentos, pesquisa por nome/marca e filtro de categoria.
- Carrinho com quantidades, cliente, Pix, dinheiro, débito ou crédito e desconto de até 30%.
- Finalização com valores calculados no serviço C#, sem aceitar preços enviados pelo cliente.
- Relatório por período e pagamento, detalhes do pedido e exportação CSV.
- Detalhes acessíveis por botão, Enter ou dois cliques; filtros alterados exigem atualização antes de exportar.
- Dados gravados em JSON por substituição atômica. Um arquivo inválido é reportado e preservado.

Não há cobrança ou integração financeira: a forma de pagamento é registrada no pedido. Não há controle de estoque nesta entrega.

## Executar no Windows (entrega principal)

Em Windows 10/11 x64, instale o **Visual Studio 2022 17.11+** com **Desenvolvimento para desktop com .NET** e o SDK .NET 8. Abra `Sonora.sln`, escolha `Sonora.Desktop` como projeto de inicialização e execute com F5.

Pelo terminal com .NET 8:

```powershell
dotnet run --project src/Sonora.Desktop
```

Para gerar uma pasta distribuível Windows x64:

```powershell
dotnet publish src/Sonora.Desktop -c Release -r win-x64 --self-contained false -p:DebugType=None -o artifacts/windows
```

Execute `Sonora.Desktop.exe` na pasta gerada. Preserve todos os arquivos da pasta. O computador de destino precisa do **.NET Desktop Runtime 8 x64**.

## Visualizar e testar na nuvem/Linux

Windows Forms não executa nativamente neste ambiente Linux. A interface ASP.NET Core de apoio implementa as mesmas operações usando `Sonora.Core`; ela não é uma renderização das janelas nativas.

```bash
bash scripts/preview.sh
```

O serviço escuta apenas a interface local na porta 5080. Não é um deploy público. As capturas geradas pelos testes podem ser consultadas em `artifacts/screenshots`.

Em uma máquina com .NET 8 já instalado, também é possível executar:

```bash
dotnet run --project src/Sonora.Preview --urls http://127.0.0.1:5080
```

## Compilar e verificar

```bash
bash scripts/check.sh
```

Ou, com o SDK ativado:

```bash
dotnet build Sonora.sln -c Release -m:2
dotnet run --project tests/Sonora.Checks -c Release
```

O projeto `Sonora.Checks` é um verificador de domínio executável, sem framework externo. Use `dotnet run`; `dotnet test` não executa essas verificações. Qualquer falha produz saída 1.

No Windows, valide os formulários nativos e gere capturas com dados temporários:

```powershell
dotnet run --project tests/Sonora.Desktop.Checks -c Release -- --screenshots artifacts/windows-native
```

As 17 verificações abrem a Splash, navegam pelas páginas, montam um pedido, conferem o total, preservam o carrinho e concluem a venda pelo botão real. Também abrem os detalhes, salvam e cancelam o seletor de arquivo CSV, conferem o arquivo gravado e testam `Ctrl+Enter` com a quantidade ainda em edição. A navegação é conferida em dois tamanhos de janela. `Form.DrawToBitmap` e `PrintWindow` produzem imagens reais dos formulários e diálogos; todos os dados ficam em um diretório temporário.

A branch `feat/finalizar-entrega` inclui três commits principais de conclusão e ajustes de validação. O workflow `Validar e integrar entrega` valida essa versão no Windows e prepara os PRs para `dev` e depois `main`. A integração respeita as regras do repositório e depende de o GitHub permitir a criação de PRs pelo token do Actions; qualquer impedimento aparece no resumo da execução.

Teste opcional da interface de apoio no Linux, com Python e Chromium instalados:

```bash
python3 -m pip install -r tests/requirements-browser.txt
bash scripts/check-browser.sh
```

O roteiro cria um arquivo temporário, sobe o serviço na porta 5081, realiza uma venda de demonstração, verifica o CSV baixado e captura as telas. Ao terminar, encerra o serviço e remove os dados temporários. Use `CHROMIUM_PATH` se o Chromium estiver fora de `/usr/bin/chromium`.

## Dados e limites

- Desktop: `%LOCALAPPDATA%\Sonora\sales.json`.
- Interface de apoio: `data/sales.json`, ignorado pelo Git. `SONORA_DATA_FILE` permite usar outro arquivo, especialmente nos testes.
- Datas e indicadores usam o fuso `America/Sao_Paulo`.
- Valores usam `decimal` e descontos arredondados para centavos.
- Este projeto é local e de um único processo gravador por arquivo. Instâncias do serviço dentro do mesmo processo compartilham a proteção do arquivo. Não compartilhe o mesmo JSON entre processos simultâneos; uma implantação multiusuário exigiria banco de dados, autenticação e transações.
- Preços, marcas e catálogo são demonstrativos.

Consulte [validação e roteiro Windows](docs/VALIDACAO.md) para distinguir o que foi verificado na nuvem do que depende de Windows. O [fluxo da equipe](docs/EQUIPE.md) descreve branches, PRs e pendências da entrega. Preencha `integrantes.txt` com os dados reais antes de entregar.
