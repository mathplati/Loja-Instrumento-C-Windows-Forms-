# Organização da entrega

## Divisão sugerida

| Responsável a definir | Escopo | Entrega |
| --- | --- | --- |
| Tech Lead | Estrutura, Splash, Menu e integração | Solução compilando e revisão dos PRs |
| Integrante de vendas | Catálogo, carrinho, validação e persistência | Venda concluída e consulta dos dados |
| Integrante de relatório | Filtros, indicadores, detalhes e CSV | Relatório consistente com as vendas |
| Integrante de qualidade | Testes, documentação e identificação | Evidências e integrantes.txt preenchido |

O usuário informou que já concluiu a identificação e a configuração da equipe. A versão final desses dados deve ser sincronizada pelo responsável, preservando nomes, RMs e usuários reais. A autoria do trabalho de cada integrante continua pertencendo ao próprio acesso.

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

Conforme a orientação mais recente do usuário, a regra é de três commits gerais por integrante. A conclusão técnica deste ambiente foi separada em três commits principais na branch `feat/finalizar-entrega`, seguidos de ajustes de validação e preservando o histórico publicado. O workflow de conclusão cria os PRs feature → `dev` e `dev` → `main` após a validação no Windows, respeitando permissões e proteções. O resumo da execução registra os números dos PRs ou o impedimento exato. A revisão feita pela automação é identificada como automatizada; aprovações humanas não são registradas por ela.
