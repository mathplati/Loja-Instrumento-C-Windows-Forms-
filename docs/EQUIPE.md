# Organização da entrega

## Divisão sugerida

| Responsável a definir | Escopo | Entrega |
| --- | --- | --- |
| Tech Lead | Estrutura, Splash, Menu e integração | Solução compilando e revisão dos PRs |
| Integrante de vendas | Catálogo, carrinho, validação e persistência | Venda concluída e consulta dos dados |
| Integrante de relatório | Filtros, indicadores, detalhes e CSV | Relatório consistente com as vendas |
| Integrante de qualidade | Testes, documentação e identificação | Evidências e integrantes.txt preenchido |

Os nomes e RMs dependem da equipe real. O código inicial não substitui os commits, revisões e PRs que cada integrante deve realizar pelo próprio acesso.

## Branches

`main` contém os três commits iniciais da estrutura. `dev` reúne a implementação em commits pequenos e descritivos. Ambas já foram enviadas ao GitHub. O desenvolvimento desktop, os testes e o workflow Windows estão em `dev`.

Para enviar novos commits depois da revisão:

```bash
git push -u origin main
git push -u origin dev
```

Para demandas seguintes, cada integrante parte de `dev`:

```bash
git switch dev
git pull --ff-only origin dev
git switch -c feat/nome-da-tarefa
# Desenvolver e registrar no mínimo três commits de trabalho real.
git push -u origin feat/nome-da-tarefa
```

Abrir PR com destino `dev`, explicar o comportamento e as verificações executadas. O Tech Lead revisa, dá feedback e integra. Ao terminar a validação no Windows, abrir PR de `dev` para `main`. Não inventar revisões, aprovações ou commits atribuídos a colegas.

## Antes da entrega

- Preencher `integrantes.txt` com todos os nomes e RMs.
- O repositório público atual é `mathplati/Loja-Instrumento-C-Windows-Forms-`, na conta pessoal `mathplati`. O usuário orientou manter este mesmo repositório. Isso é uma escolha diferente da organização pedida no enunciado original.
- Conceder Write aos integrantes.
- Conferir os PRs para `dev` e o histórico de cada tarefa.
- Executar o roteiro Windows em `docs/VALIDACAO.md`.
- Guardar os dados de demonstração separados dos dados reais.

## Pendências da equipe

Os nomes, RMs, usuários do GitHub e responsáveis reais ainda não foram informados. Os commits atuais utilizam a identidade Git já configurada; não comprovam participação individual da equipe. Cada integrante precisa realizar seus próprios commits e PRs para cumprir o fluxo acadêmico. Até a auditoria realizada nesta implementação, o repositório não tinha PRs abertos ou fechados nem revisão de colegas registrada. Não foram criadas aprovações ou identidades fictícias.
