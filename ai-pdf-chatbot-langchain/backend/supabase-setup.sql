create extension if not exists vector with schema extensions;

create table if not exists public.documents (
  id bigserial primary key,
  content text,
  metadata jsonb,
  embedding extensions.vector(1536)
);
alter table public.documents enable row level security;
grant select, insert, update, delete on public.documents to service_role;
grant usage, select on sequence public.documents_id_seq to service_role;

create or replace function public.match_documents (
  query_embedding extensions.vector(1536),
  match_count int default null,
  filter jsonb default '{}'
) returns table (id bigint, content text, metadata jsonb, similarity float)
language sql stable set search_path = public, extensions as $$
  select id, content, metadata,
    1 - (embedding <=> query_embedding) as similarity
  from public.documents
  where metadata @> filter
  order by embedding <=> query_embedding
  limit match_count;
$$;
revoke all on function public.match_documents(extensions.vector, int, jsonb) from public;
grant execute on function public.match_documents(extensions.vector, int, jsonb) to service_role;
